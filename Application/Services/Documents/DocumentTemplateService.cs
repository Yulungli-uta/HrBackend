using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.Templates;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Documents.Abstractions;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services.Documents;

/// <summary>
/// Servicio de gestión de plantillas documentales.
/// Implementa CRUD completo y la previsualización de plantillas con datos reales.
/// </summary>
public sealed class DocumentTemplateService : IDocumentTemplateService
{
    private readonly IDocumentTemplateRepository _templateRepository;
    private readonly IDocumentTemplateFieldRepository _fieldRepository;
    private readonly IDocumentFieldResolver _fieldResolver;
    private readonly IDocumentTemplateEngine _templateEngine;
    private readonly ILogger<DocumentTemplateService> _logger;

    public DocumentTemplateService(
        IDocumentTemplateRepository templateRepository,
        IDocumentTemplateFieldRepository fieldRepository,
        IDocumentFieldResolver fieldResolver,
        IDocumentTemplateEngine templateEngine,
        ILogger<DocumentTemplateService> logger)
    {
        _templateRepository = templateRepository;
        _fieldRepository    = fieldRepository;
        _fieldResolver      = fieldResolver;
        _templateEngine     = templateEngine;
        _logger             = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DocumentTemplateSummaryDto>> GetAllAsync(
        string? templateType = null,
        DocumentTemplateStatus? status = null,
        CancellationToken ct = default)
    {
        return await _templateRepository.GetAllAsync(templateType, status, ct);
    }

    /// <inheritdoc />
    public async Task<DocumentTemplateDetailDto> GetDetailByIdAsync(
        int templateId,
        CancellationToken ct = default)
    {
        return await _templateRepository.GetDetailByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {templateId} no encontrada.");
    }

    /// <inheritdoc />
    public async Task<int> CreateAsync(
        CreateDocumentTemplateRequest request,
        int createdBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var codeNormalized = request.TemplateCode.ToUpperInvariant().Trim();

        var exists = await _templateRepository.ExistsByCodeAsync(codeNormalized, ct: ct);
        if (exists)
            throw new InvalidOperationException(
                $"Ya existe una plantilla con el código '{request.TemplateCode}'.");

        var template = new DocumentTemplate
        {
            TemplateCode      = codeNormalized,
            Name              = request.Name.Trim(),
            Description       = request.Description?.Trim(),
            TemplateType      = request.TemplateType.ToUpperInvariant().Trim(),
            Version           = request.Version.Trim(),
            LayoutType        = request.LayoutType,
            Status            = DocumentTemplateStatus.Draft,
            HtmlContent       = request.HtmlContent,
            CssStyles         = request.CssStyles,
            MetaJson          = request.MetaJson,
            RequiresSignature = request.RequiresSignature,
            RequiresApproval  = request.RequiresApproval,
            CreatedAt         = DateTime.UtcNow,
            CreatedBy         = createdBy
        };

        var templateId = await _templateRepository.CreateAsync(template, ct);

        if (request.Fields is { Count: > 0 })
        {
            var fields = request.Fields.Select((f, i) => new DocumentTemplateField
            {
                TemplateId     = templateId,
                FieldName      = f.FieldName.ToUpperInvariant().Trim(),
                Label          = f.Label.Trim(),
                SourceType     = f.SourceType,
                SourceProperty = f.SourceProperty?.Trim(),
                DataType       = f.DataType.Trim(),
                FormatPattern  = f.FormatPattern?.Trim(),
                DefaultValue   = f.DefaultValue?.Trim(),
                IsRequired     = f.IsRequired,
                IsEditable     = f.IsEditable,
                SortOrder      = f.SortOrder > 0 ? f.SortOrder : i + 1,
                HelpText       = f.HelpText?.Trim()
            }).ToList();

            foreach (var field in fields)
                await _fieldRepository.CreateAsync(field, ct);
        }

        _logger.LogInformation(
            "DocumentTemplateService: plantilla '{Code}' creada con ID {Id} por usuario {User}.",
            codeNormalized, templateId, createdBy);

        return templateId;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        int templateId,
        UpdateDocumentTemplateRequest request,
        int updatedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await _templateRepository.GetByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {templateId} no encontrada.");

        if (template.Status == DocumentTemplateStatus.Archived)
            throw new InvalidOperationException("No se puede modificar una plantilla archivada.");

        template.Name              = request.Name.Trim();
        template.Description       = request.Description?.Trim();
        template.Version           = request.Version.Trim();
        template.LayoutType        = request.LayoutType;
        template.HtmlContent       = request.HtmlContent;
        template.CssStyles         = request.CssStyles;
        template.MetaJson          = request.MetaJson;
        template.RequiresSignature = request.RequiresSignature;
        template.RequiresApproval  = request.RequiresApproval;
        template.UpdatedAt         = DateTime.UtcNow;
        template.UpdatedBy         = updatedBy;

        await _templateRepository.UpdateAsync(template, ct);

        _logger.LogInformation(
            "DocumentTemplateService: plantilla {Id} actualizada por usuario {User}.",
            templateId, updatedBy);
    }

    /// <inheritdoc />
    public async Task ChangeStatusAsync(
        int templateId,
        ChangeTemplateStatusRequest request,
        int updatedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await _templateRepository.GetByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {templateId} no encontrada.");

        var currentStatus = template.Status;
        var newStatus     = request.Status;

        var validTransitions = new Dictionary<DocumentTemplateStatus, DocumentTemplateStatus[]>
        {
            [DocumentTemplateStatus.Draft]     = [DocumentTemplateStatus.Published],
            [DocumentTemplateStatus.Published] = [DocumentTemplateStatus.Draft, DocumentTemplateStatus.Archived],
            [DocumentTemplateStatus.Archived]  = []
        };

        if (!validTransitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Transición de estado inválida: {currentStatus} → {newStatus}.");

        await _templateRepository.UpdateStatusAsync(templateId, newStatus, ct);

        _logger.LogInformation(
            "DocumentTemplateService: plantilla {Id} cambió de {From} a {To} por usuario {User}.",
            templateId, currentStatus, newStatus, updatedBy);
    }

    /// <inheritdoc />
    public async Task<PreviewTemplateResponse> PreviewAsync(
        PreviewTemplateRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await _templateRepository.GetDetailByIdAsync(request.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {request.TemplateId} no encontrada.");

        var fields = await _fieldRepository.GetByTemplateIdAsync(request.TemplateId, ct);

        Dictionary<string, string> resolvedValues;

        if (request.EmployeeId.HasValue)
        {
            resolvedValues = await _fieldResolver.ResolveAsync(
                fields,
                request.EmployeeId.Value,
                request.EntityId,
                request.ManualOverrides,
                ct);
        }
        else
        {
            // Datos de muestra cuando no se especifica empleado
            resolvedValues = fields.ToDictionary(
                f => f.FieldName,
                f => f.DefaultValue ?? $"[{f.Label}]");
        }

        // Los overrides manuales tienen prioridad máxima
        if (request.ManualOverrides is { Count: > 0 })
        {
            foreach (var (key, value) in request.ManualOverrides)
                resolvedValues[key.ToUpperInvariant()] = value;
        }

        var renderedHtml = _templateEngine.Render(template.HtmlContent, resolvedValues);
        var tokens       = _templateEngine.ExtractTokens(template.HtmlContent);

        var unresolvedFields = tokens
            .Where(t => !resolvedValues.ContainsKey(t) || string.IsNullOrEmpty(resolvedValues[t]))
            .Select(t =>
            {
                var fieldDef = fields.FirstOrDefault(f => f.FieldName == t);
                return new UnresolvedFieldDto(
                    FieldName: t,
                    Label:     fieldDef?.Label ?? t,
                    Reason:    fieldDef is null
                               ? "Campo no definido en la plantilla"
                               : "No se pudo resolver el valor");
            })
            .ToList();

        return new PreviewTemplateResponse(
            HtmlContent:      renderedHtml,
            UnresolvedFields: unresolvedFields);
    }
}

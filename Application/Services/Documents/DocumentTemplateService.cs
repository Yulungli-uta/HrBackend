using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.Templates;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Models;
using WsUtaSystem.Reports.Abstractions;

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

        // Al publicar: archivar otras versiones Published del mismo TemplateCode (el índice único
        // filtrado UX_DocumentTemplates_TemplateCode_Published exige que solo una esté Published).
        // Además, repuntar ContractType.DefaultTemplateId/DelegationTemplateId y
        // PersonnelActionType.DefaultTemplateId de las versiones archivadas hacia la nueva versión
        // publicada, para que ningún consumidor quede apuntando a un TemplateId archivado
        // (ResolveContractTemplateIdAsync/GenerateDocumentForActionAsync exigen Published).
        if (newStatus == DocumentTemplateStatus.Published)
        {
            var siblings = await _templateRepository.GetVersionsByCodeAsync(template.TemplateCode, ct);
            var previouslyPublishedIds = siblings
                .Where(v => v.TemplateId != templateId && v.Status == DocumentTemplateStatus.Published)
                .Select(v => v.TemplateId)
                .ToList();

            await _templateRepository.ArchiveOtherPublishedVersionsAsync(template.TemplateCode, templateId, ct);

            foreach (var oldTemplateId in previouslyPublishedIds)
                await _templateRepository.RepointTemplateConsumersAsync(oldTemplateId, templateId, ct);
        }

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

        // El resolver ya soporta employeeId nulo (omite las fuentes Employee/Contract/Movement
        // pero sí resuelve los campos System, como LOGO_URL o INSTITUTION_NAME, que no dependen
        // de un empleado). Llamarlo siempre evita que la vista previa "de muestra" rompa esos
        // campos institucionales mostrando el placeholder literal en vez del valor real.
        var resolvedValues = await _fieldResolver.ResolveAsync(
            fields,
            request.EmployeeId,
            request.EntityId,
            request.ManualOverrides,
            ct);

        if (!request.EmployeeId.HasValue)
        {
            // Datos de muestra para los campos que sí dependen de un empleado/contrato y que el
            // resolver no pudo completar sin esa información — solo rellena los vacíos, nunca
            // sobreescribe un valor que el resolver sí logró resolver (ej. campos System).
            foreach (var field in fields)
            {
                if (!resolvedValues.TryGetValue(field.FieldName, out var value) || string.IsNullOrEmpty(value))
                    resolvedValues[field.FieldName] = field.DefaultValue ?? $"[{field.Label}]";
            }
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

    /// <inheritdoc />
    public async Task<CreateVersionResponse> CreateVersionAsync(
        int sourceTemplateId,
        string newVersion,
        int createdBy,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newVersion);

        var source = await _templateRepository.GetByIdAsync(sourceTemplateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {sourceTemplateId} no encontrada.");

        var versionTrimmed = newVersion.Trim();

        // Verificar que no exista ya esa versión exacta para el mismo código
        var existing = await _templateRepository.GetVersionsByCodeAsync(source.TemplateCode, ct);
        if (existing.Any(v => string.Equals(v.Version, versionTrimmed, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                $"Ya existe la versión '{versionTrimmed}' para el código '{source.TemplateCode}'.");

        // No permitir crear otra versión si ya hay una Draft sin terminar (publicar/asignar):
        // evita que se acumulen borradores abandonados sin completar su ciclo de vida.
        var pendingDraft = existing.FirstOrDefault(v => v.Status == DocumentTemplateStatus.Draft);
        if (pendingDraft is not null)
            throw new InvalidOperationException(
                $"Ya existe una versión '{pendingDraft.Version}' en Borrador sin publicar para '{source.TemplateCode}'. " +
                "Termine de editarla y publíquela antes de crear una nueva versión.");

        var newTemplate = new DocumentTemplate
        {
            TemplateCode      = source.TemplateCode,
            Name              = source.Name,
            Description       = source.Description,
            TemplateType      = source.TemplateType,
            Version           = versionTrimmed,
            LayoutType        = source.LayoutType,
            Status            = DocumentTemplateStatus.Draft,
            HtmlContent       = source.HtmlContent,
            CssStyles         = source.CssStyles,
            MetaJson          = source.MetaJson,
            RequiresSignature = source.RequiresSignature,
            RequiresApproval  = source.RequiresApproval,
            CreatedAt         = DateTime.UtcNow,
            CreatedBy         = createdBy
        };

        var newId = await _templateRepository.CreateAsync(newTemplate, ct);

        // Copiar los campos de la versión origen
        var sourceFields = await _fieldRepository.GetByTemplateIdAsync(sourceTemplateId, ct);
        foreach (var f in sourceFields)
        {
            await _fieldRepository.CreateAsync(new DocumentTemplateField
            {
                TemplateId     = newId,
                FieldName      = f.FieldName,
                Label          = f.Label,
                SourceType     = f.SourceType,
                SourceProperty = f.SourceProperty,
                DataType       = f.DataType,
                FormatPattern  = f.FormatPattern,
                DefaultValue   = f.DefaultValue,
                IsRequired     = f.IsRequired,
                IsEditable     = f.IsEditable,
                SortOrder      = f.SortOrder,
                HelpText       = f.HelpText
            }, ct);
        }

        _logger.LogInformation(
            "DocumentTemplateService: nueva versión '{Version}' de '{Code}' creada con ID {Id} por usuario {User}.",
            versionTrimmed, source.TemplateCode, newId, createdBy);

        return new CreateVersionResponse(newId, versionTrimmed, source.TemplateCode);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateVersionSummaryDto>> GetVersionsByCodeAsync(
        string templateCode,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateCode);
        return await _templateRepository.GetVersionsByCodeAsync(templateCode.ToUpperInvariant().Trim(), ct);
    }

    /// <inheritdoc />
    public async Task<ImportContractTextResponse> ImportContractTextAsync(int contractTypeId, CancellationToken ct = default)
    {
        var row = await _templateRepository.GetContractTypeTextAsync(contractTypeId, ct)
            ?? throw new KeyNotFoundException($"Tipo de contrato {contractTypeId} no encontrado.");

        var rawText = row.ContractText ?? string.Empty;

        // Detectar placeholders legados {0}, {1}, {9_1}, etc.
        var matches = System.Text.RegularExpressions.Regex.Matches(rawText, @"\{(\d+(?:_\d+)?)\}");

        var placeholders = matches
            .GroupBy(m => m.Value)
            .Select(g =>
            {
                var firstMatch = g.First();
                var start = Math.Max(0, firstMatch.Index - 40);
                var length = Math.Min(80, rawText.Length - start);
                return new LegacyPlaceholderDto(
                    Placeholder: g.Key,
                    Occurrences: g.Count(),
                    Context: rawText.Substring(start, length).Replace("\n", " ").Replace("\r", ""));
            })
            .OrderBy(p => p.Placeholder)
            .ToList();

        return new ImportContractTextResponse(
            ContractTypeId:   row.ContractTypeId,
            ContractTypeName: row.ContractTypeName,
            RawText:          rawText,
            Placeholders:     placeholders);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateContractTypeOptionDto>> GetContractTypesForTemplateAsync(int templateId, CancellationToken ct = default)
        => await _templateRepository.GetContractTypesForTemplateAsync(templateId, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TemplateActionTypeOptionDto>> GetActionTypesForTemplateAsync(int templateId, CancellationToken ct = default)
        => await _templateRepository.GetActionTypesForTemplateAsync(templateId, ct);

    /// <inheritdoc />
    public ExtractTokensResponse ExtractTokens(ExtractTokensRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tokens = _templateEngine.ExtractTokens(request.HtmlContent);
        return new ExtractTokensResponse(tokens);
    }
}

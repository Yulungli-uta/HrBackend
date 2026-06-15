using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Models;
using WsUtaSystem.Reports.Abstractions;

namespace WsUtaSystem.Application.Services.Documents;

/// <summary>
/// Servicio orquestador de generación de documentos PDF institucionales.
/// Coordina el flujo completo: resolución de campos → sustitución de placeholders
/// → renderizado PDF → persistencia del snapshot y registro del documento.
/// </summary>
public sealed class DocumentGenerationService : IDocumentGenerationService
{
    private readonly IDocumentTemplateRepository _templateRepository;
    private readonly IDocumentTemplateFieldRepository _fieldRepository;
    private readonly IGeneratedDocumentRepository _documentRepository;
    private readonly IDocumentFieldResolver _fieldResolver;
    private readonly IDocumentTemplateEngine _templateEngine;
    private readonly IDocumentRendererFactory _rendererFactory;
    private readonly ILogger<DocumentGenerationService> _logger;

    public DocumentGenerationService(
        IDocumentTemplateRepository templateRepository,
        IDocumentTemplateFieldRepository fieldRepository,
        IGeneratedDocumentRepository documentRepository,
        IDocumentFieldResolver fieldResolver,
        IDocumentTemplateEngine templateEngine,
        IDocumentRendererFactory rendererFactory,
        ILogger<DocumentGenerationService> logger)
    {
        _templateRepository = templateRepository;
        _fieldRepository = fieldRepository;
        _documentRepository = documentRepository;
        _fieldResolver = fieldResolver;
        _templateEngine = templateEngine;
        _rendererFactory = rendererFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GenerateDocumentResponse> GenerateAsync(
        GenerateDocumentRequest request,
        int generatedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "DocumentGenerationService: iniciando generación de documento para plantilla {TemplateId}, empleado {EmployeeId}.",
            request.TemplateId, request.EmployeeId);

        var template = await _templateRepository.GetByIdAsync(request.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {request.TemplateId} no encontrada.");

        if (template.Status != DocumentTemplateStatus.Published)
            throw new InvalidOperationException(
                $"La plantilla '{template.Name}' no está publicada. Estado actual: {template.Status}.");

        var fields = await _fieldRepository.GetByTemplateIdAsync(request.TemplateId, ct);

        var resolvedValues = await _fieldResolver.ResolveAsync(
            fields,
            request.EmployeeId,
            request.EntityId,
            request.ManualOverrides,
            ct,
            request.PersonId);

        // Aplicar overrides manuales directamente al dict de valores resueltos.
        // Esto cubre tokens del HTML que no tienen definición en tbl_DocumentTemplateFields.
        if (request.ManualOverrides is not null)
        {
            _logger.LogInformation(
                "DocumentGenerationService: aplicando {Count} overrides manuales. Keys: [{Keys}]",
                request.ManualOverrides.Count,
                string.Join(", ", request.ManualOverrides.Keys));

            foreach (var kv in request.ManualOverrides)
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    resolvedValues[kv.Key] = kv.Value;
        }
        else
        {
            _logger.LogWarning(
                "DocumentGenerationService: sin overrides manuales para plantilla {TemplateId}.", request.TemplateId);
        }

        _logger.LogInformation(
            "DocumentGenerationService: valores resueltos ({Count}): [{Values}]",
            resolvedValues.Count,
            string.Join("; ", resolvedValues.Select(kv => $"{kv.Key}={kv.Value}")));

        var unresolvedFields = fields
            .Where(f => f.IsRequired
                     && (!resolvedValues.TryGetValue(f.FieldName, out var val)
                         || string.IsNullOrWhiteSpace(val)))
            .Select(f => new UnresolvedFieldInfo(
                FieldName: f.FieldName,
                Label: f.Label,
                DefaultValueUsed: f.DefaultValue))
            .ToList();

        if (unresolvedFields.Count > 0)
        {
            _logger.LogWarning(
                "DocumentGenerationService: {Count} campos requeridos sin resolver para plantilla {TemplateId}. Campos: [{Fields}]",
                unresolvedFields.Count, request.TemplateId,
                string.Join(", ", unresolvedFields.Select(f => f.FieldName)));
        }

        var renderedHtml = _templateEngine.Render(template.HtmlContent, resolvedValues);

        var renderer = _rendererFactory.GetRenderer(template.TemplateType);
        var pdfBytes = await renderer.RenderToPdfAsync(renderedHtml, template.CssStyles);

        var docNumber = request.DocumentNumber
            ?? $"DOC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var fileName = BuildFileName(template.TemplateCode, docNumber);

        var generatedDocument = new GeneratedDocument
        {
            TemplateId = request.TemplateId,
            EmployeeId = request.EmployeeId,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            DocumentNumber = docNumber,
            FileName = fileName,
            Status = "GENERATED",
            Notes = request.Notes,
            TemplateVersion = template.Version,
            HtmlSnapshot = renderedHtml,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = generatedBy,
            Fields = fields.Select(f =>
            {
                resolvedValues.TryGetValue(f.FieldName, out var fieldValue);
                var wasOverridden = request.ManualOverrides is not null
                                 && request.ManualOverrides.ContainsKey(f.FieldName);
                return new GeneratedDocumentField
                {
                    FieldName = f.FieldName,
                    FieldValue = fieldValue,
                    SourceType = f.SourceType.ToString(),
                    WasOverridden = wasOverridden
                };
            }).ToList()
        };

        var documentId = await _documentRepository.CreateAsync(generatedDocument, ct);

        _logger.LogInformation(
            "DocumentGenerationService: documento {DocId} generado correctamente. Archivo: {FileName}, Tamaño: {Size} bytes.",
            documentId, fileName, pdfBytes.Length);

        return new GenerateDocumentResponse(
            DocumentId: documentId,
            DocumentNumber: docNumber,
            FileName: fileName,
            PdfBase64: Convert.ToBase64String(pdfBytes),
            FileSizeBytes: pdfBytes.Length,
            UnresolvedFields: unresolvedFields);
    }

    /// <inheritdoc />
    public async Task<PagedDocumentResult> GetPagedAsync(
        DocumentQueryFilter filter,
        CancellationToken ct = default)
    {
        return await _documentRepository.GetPagedAsync(filter, ct);
    }

    /// <inheritdoc />
    public async Task<GeneratedDocumentDetailDto> GetDetailByIdAsync(
        int documentId,
        CancellationToken ct = default)
    {
        return await _documentRepository.GetDetailByIdAsync(documentId, ct)
            ?? throw new KeyNotFoundException($"Documento generado {documentId} no encontrado.");
    }

    /// <inheritdoc />
    public async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadAsync(
        int documentId,
        CancellationToken ct = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, ct)
            ?? throw new KeyNotFoundException($"Documento generado {documentId} no encontrado.");

        string renderedHtml;
        string? cssStyles;
        string? templateType;

        if (!string.IsNullOrWhiteSpace(document.HtmlSnapshot))
        {
            // Usa el snapshot inmutable guardado al momento de generación — no depende del estado actual de la plantilla
            renderedHtml = document.HtmlSnapshot;

            var tmpl = await _templateRepository.GetByIdAsync(document.TemplateId, ct);
            cssStyles    = tmpl?.CssStyles;
            templateType = tmpl?.TemplateType;
        }
        else
        {
            // Compatibilidad: documentos generados antes de implementar HtmlSnapshot
            var detail = await _documentRepository.GetDetailByIdAsync(documentId, ct)
                ?? throw new KeyNotFoundException($"Detalle del documento {documentId} no encontrado.");

            var template = await _templateRepository.GetByIdAsync(document.TemplateId, ct)
                ?? throw new KeyNotFoundException($"Plantilla {document.TemplateId} no encontrada.");

            var snapshotValues = detail.Fields.ToDictionary(
                f => f.FieldName,
                f => f.FieldValue ?? string.Empty);

            renderedHtml = _templateEngine.Render(template.HtmlContent, snapshotValues);
            cssStyles    = template.CssStyles;
            templateType = template.TemplateType;
        }

        var renderer = _rendererFactory.GetRenderer(templateType);
        var pdfBytes = await renderer.RenderToPdfAsync(renderedHtml, cssStyles);

        return (pdfBytes, document.FileName, "application/pdf");
    }

    /// <inheritdoc />
    public async Task ApproveAsync(
        int documentId,
        ApproveDocumentRequest request,
        int approvedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var document = await _documentRepository.GetByIdAsync(documentId, ct)
            ?? throw new KeyNotFoundException($"Documento generado {documentId} no encontrado.");

        if (document.IsApproved)
            throw new InvalidOperationException($"El documento {documentId} ya está aprobado.");

        if (document.Status == "REJECTED" || document.Status == "ARCHIVED")
            throw new InvalidOperationException(
                $"No se puede aprobar un documento en estado '{document.Status}'.");

        await _documentRepository.ApproveAsync(documentId, approvedBy, request.Notes, ct);

        _logger.LogInformation(
            "DocumentGenerationService: documento {DocId} aprobado por usuario {User}.",
            documentId, approvedBy);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(
        int documentId,
        UpdateDocumentStatusRequest request,
        int updatedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _ = await _documentRepository.GetByIdAsync(documentId, ct)
            ?? throw new KeyNotFoundException($"Documento generado {documentId} no encontrado.");

        var validStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DRAFT", "GENERATED", "SIGNED", "APPROVED", "REJECTED", "ARCHIVED"
        };

        if (!validStatuses.Contains(request.Status))
            throw new InvalidOperationException($"Estado '{request.Status}' no válido.");

        await _documentRepository.UpdateStatusAsync(documentId, request.Status.ToUpperInvariant(), request.Notes, ct);

        _logger.LogInformation(
            "DocumentGenerationService: documento {DocId} actualizado a estado '{Status}' por usuario {User}.",
            documentId, request.Status, updatedBy);
    }

    /// <inheritdoc />
    public async Task<string> PreviewAsync(
        int templateId,
        int employeeId,
        Dictionary<string, string> overrides,
        CancellationToken ct = default)
    {
        var template = await _templateRepository.GetByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {templateId} no encontrada.");
        _logger.LogInformation(
            "Template usada para PDF. TemplateId={TemplateId}, Code={Code}, Name={Name}, Type={Type}, HtmlStart={HtmlStart}",
            template.TemplateId,
            template.TemplateCode,
            template.Name,
            template.TemplateType,
            template.HtmlContent != null && template.HtmlContent.Length > 500
                ? template.HtmlContent.Substring(0, 500)
                : template.HtmlContent);

        var fields = await _fieldRepository.GetByTemplateIdAsync(templateId, ct);

        var resolvedValues = await _fieldResolver.ResolveAsync(fields, employeeId, null, overrides, ct);

        foreach (var kv in overrides)
            if (!string.IsNullOrWhiteSpace(kv.Value))
                resolvedValues[kv.Key] = kv.Value;

        _logger.LogInformation(
            "Preview resolved values. Count={Count}, Values={Values}",
            resolvedValues.Count,
            string.Join("; ", resolvedValues.Select(x => $"{x.Key}={x.Value}")));

        var renderedHtml = _templateEngine.Render(template.HtmlContent, resolvedValues);
        var renderer = _rendererFactory.GetRenderer(template.TemplateType);
        var pdfBytes = await renderer.RenderToPdfAsync(renderedHtml, template.CssStyles);

        _logger.LogInformation(
            "Preview rendered HTML. HasUnresolvedTokens={HasTokens}, HasMeta={HasMeta}, Sample={Sample}",
            renderedHtml.Contains("{{"),
            renderedHtml.Contains("<meta", StringComparison.OrdinalIgnoreCase),
            renderedHtml.Length > 1000 ? renderedHtml[..1000] : renderedHtml);

        _logger.LogInformation(
            "DocumentGenerationService: previsualización generada para plantilla {TemplateId}, empleado {EmployeeId}. Tamaño: {Size} bytes.",
            templateId, employeeId, pdfBytes.Length);

        return Convert.ToBase64String(pdfBytes);
    }

    private static string BuildFileName(string templateCode, string documentNumber)
    {
        var safeName = string.Concat(
            documentNumber.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

        return $"{templateCode}_{safeName}_{DateTime.UtcNow:yyyyMMdd}.pdf";
    }
}
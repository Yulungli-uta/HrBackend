using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Documents.Abstractions;
using WsUtaSystem.Models;

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
    private readonly IDocumentRenderer _renderer;
    private readonly ILogger<DocumentGenerationService> _logger;

    public DocumentGenerationService(
        IDocumentTemplateRepository templateRepository,
        IDocumentTemplateFieldRepository fieldRepository,
        IGeneratedDocumentRepository documentRepository,
        IDocumentFieldResolver fieldResolver,
        IDocumentTemplateEngine templateEngine,
        IDocumentRenderer renderer,
        ILogger<DocumentGenerationService> logger)
    {
        _templateRepository = templateRepository;
        _fieldRepository    = fieldRepository;
        _documentRepository = documentRepository;
        _fieldResolver      = fieldResolver;
        _templateEngine     = templateEngine;
        _renderer           = renderer;
        _logger             = logger;
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

        // ── 1. Obtener plantilla con sus campos ──────────────────────────────────
        var template = await _templateRepository.GetByIdAsync(request.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {request.TemplateId} no encontrada.");

        if (template.Status != DocumentTemplateStatus.Published)
            throw new InvalidOperationException(
                $"La plantilla '{template.Name}' no está publicada. Estado actual: {template.Status}.");

        var fields = await _fieldRepository.GetByTemplateIdAsync(request.TemplateId, ct);

        // ── 2. Resolver campos desde las fuentes de datos ────────────────────────
        var resolvedValues = await _fieldResolver.ResolveAsync(
            fields,
            request.EmployeeId,
            request.EntityId,
            request.ManualOverrides,
            ct);

        // ── 3. Identificar campos no resueltos ───────────────────────────────────
        var unresolvedFields = fields
            .Where(f => f.IsRequired
                     && (!resolvedValues.TryGetValue(f.FieldName, out var val)
                         || string.IsNullOrWhiteSpace(val)))
            .Select(f => new UnresolvedFieldInfo(
                FieldName:        f.FieldName,
                Label:            f.Label,
                DefaultValueUsed: f.DefaultValue))
            .ToList();

        if (unresolvedFields.Count > 0)
        {
            _logger.LogWarning(
                "DocumentGenerationService: {Count} campos requeridos sin resolver para plantilla {TemplateId}.",
                unresolvedFields.Count, request.TemplateId);
        }

        // ── 4. Sustituir placeholders en el HTML ─────────────────────────────────
        var renderedHtml = _templateEngine.Render(template.HtmlContent, resolvedValues);

        // ── 5. Renderizar a PDF ──────────────────────────────────────────────────
        var pdfBytes = await _renderer.RenderToPdfAsync(renderedHtml, template.CssStyles);

        // ── 6. Generar nombre de archivo ─────────────────────────────────────────
        var docNumber = request.DocumentNumber
            ?? $"DOC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var fileName = BuildFileName(template.TemplateCode, docNumber);

        // ── 7. Crear registro del documento generado ─────────────────────────────
        var generatedDocument = new GeneratedDocument
        {
            TemplateId     = request.TemplateId,
            EmployeeId     = request.EmployeeId,
            EntityType     = request.EntityType,
            EntityId       = request.EntityId,
            DocumentNumber = docNumber,
            FileName       = fileName,
            Status         = "GENERATED",
            Notes          = request.Notes,
            CreatedAt      = DateTime.UtcNow,
            CreatedBy      = generatedBy,
            // Snapshot inmutable de los valores resueltos
            Fields = fields.Select(f =>
            {
                resolvedValues.TryGetValue(f.FieldName, out var fieldValue);
                var wasOverridden = request.ManualOverrides is not null
                                 && request.ManualOverrides.ContainsKey(f.FieldName);
                return new GeneratedDocumentField
                {
                    FieldName    = f.FieldName,
                    FieldValue   = fieldValue,
                    SourceType   = f.SourceType.ToString(),
                    WasOverridden = wasOverridden
                };
            }).ToList()
        };

        var documentId = await _documentRepository.CreateAsync(generatedDocument, ct);

        _logger.LogInformation(
            "DocumentGenerationService: documento {DocId} generado correctamente. Archivo: {FileName}, Tamaño: {Size} bytes.",
            documentId, fileName, pdfBytes.Length);

        return new GenerateDocumentResponse(
            DocumentId:       documentId,
            DocumentNumber:   docNumber,
            FileName:         fileName,
            PdfBase64:        Convert.ToBase64String(pdfBytes),
            FileSizeBytes:    pdfBytes.Length,
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

        // El PDF se regenera a partir del snapshot de campos guardado
        var detail = await _documentRepository.GetDetailByIdAsync(documentId, ct)
            ?? throw new KeyNotFoundException($"Detalle del documento {documentId} no encontrado.");

        var template = await _templateRepository.GetByIdAsync(document.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {document.TemplateId} no encontrada.");

        // Reconstruir valores desde el snapshot
        var snapshotValues = detail.Fields.ToDictionary(
            f => f.FieldName,
            f => f.FieldValue ?? string.Empty);

        var renderedHtml = _templateEngine.Render(template.HtmlContent, snapshotValues);
        var pdfBytes     = await _renderer.RenderToPdfAsync(renderedHtml, template.CssStyles);

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

    // ── Métodos privados ─────────────────────────────────────────────────────────

    private static string BuildFileName(string templateCode, string documentNumber)
    {
        // Sanitizar caracteres no válidos para nombres de archivo
        var safeName = string.Concat(
            documentNumber.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));

        return $"{templateCode}_{safeName}_{DateTime.UtcNow:yyyyMMdd}.pdf";
    }
}

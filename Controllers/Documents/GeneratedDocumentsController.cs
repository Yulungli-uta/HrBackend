// ============================================================
// WsUtaSystem.Controllers.Documents.GeneratedDocumentsController
// Motor Documental Institucional — Generación y Descarga de PDF
// ============================================================
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.Interfaces.Services.Documents;

namespace WsUtaSystem.Controllers.Documents;

/// <summary>
/// Expone los endpoints REST para la generación, consulta, aprobación
/// y descarga de documentos PDF institucionales generados a partir de plantillas.
///
/// Rutas base: <c>api/v1/documents/generated</c>
/// </summary>
[ApiController]
[Route("generated")]
public sealed class GeneratedDocumentsController : ControllerBase
{
    private readonly IDocumentGenerationService _generationService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GeneratedDocumentsController> _logger;

    public GeneratedDocumentsController(
        IDocumentGenerationService generationService,
        ICurrentUserService currentUser,
        ILogger<GeneratedDocumentsController> logger)
    {
        _generationService = generationService;
        _currentUser       = currentUser;
        _logger            = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CONSULTAS
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene una lista paginada de documentos generados con filtros opcionales.
    /// Permite filtrar por plantilla, entidad, estado y rango de fechas.
    /// </summary>
    /// <param name="filter">Parámetros de búsqueda y paginación.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedDocumentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] DocumentQueryFilter filter,
        CancellationToken ct)
    {
        var result = await _generationService.GetPagedAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle completo de un documento generado,
    /// incluyendo el snapshot de campos resueltos en el momento de generación.
    /// </summary>
    /// <param name="id">Identificador del documento generado.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GeneratedDocumentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var document = await _generationService.GetDetailByIdAsync(id, ct);
        return document is null ? NotFound() : Ok(document);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GENERACIÓN
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Genera un documento PDF a partir de una plantilla y un contexto de entidad.
    /// Resuelve automáticamente los campos desde las fuentes configuradas
    /// (empleado, contrato, movimiento, sistema) y permite sobreescrituras manuales.
    /// </summary>
    /// <param name="request">Datos de generación: plantilla, entidad y overrides.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost]
    [ProducesResponseType(typeof(GenerateDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateDocumentRequest request,
        CancellationToken ct)
    {
        var generatedBy = _currentUser.EmployeeId ?? 0;
        var result = await _generationService.GenerateAsync(request, generatedBy, ct);

        _logger.LogInformation(
            "Documento PDF generado. DocumentId={DocId} TemplateId={TemplateId} EntityId={EntityId} por EmployeeId={UserId}",
            result.DocumentId, request.TemplateId, request.EntityId, generatedBy);

        return CreatedAtAction(nameof(GetById), new { id = result.DocumentId }, result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DESCARGA
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Descarga el archivo PDF de un documento generado.
    /// Retorna el binario con el Content-Type y nombre de archivo correctos.
    /// </summary>
    /// <param name="id">Identificador del documento generado.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("{id:int}/download")]
    [Produces(MediaTypeNames.Application.Pdf)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download([FromRoute] int id, CancellationToken ct)
    {
        var (bytes, fileName, contentType) = await _generationService.DownloadAsync(id, ct);

        _logger.LogInformation(
            "Descarga de documento PDF. DocumentId={DocId} FileName={FileName} por EmployeeId={UserId}",
            id, fileName, _currentUser.EmployeeId);

        return File(bytes, contentType, fileName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GESTIÓN DE ESTADO
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Aprueba un documento generado, registrando el aprobador y observaciones.
    /// Cambia el estado del documento a <c>Approved</c>.
    /// </summary>
    /// <param name="id">Identificador del documento generado.</param>
    /// <param name="request">Datos de aprobación (observaciones, firma digital).</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        [FromRoute] int id,
        [FromBody] ApproveDocumentRequest request,
        CancellationToken ct)
    {
        var approvedBy = _currentUser.EmployeeId ?? 0;
        await _generationService.ApproveAsync(id, request, approvedBy, ct);

        _logger.LogInformation(
            "Documento Id={DocId} aprobado por EmployeeId={UserId}",
            id, approvedBy);

        return NoContent();
    }

    /// <summary>
    /// Actualiza el estado de un documento generado (Draft, Pending, Approved, Rejected, Voided).
    /// </summary>
    /// <param name="id">Identificador del documento generado.</param>
    /// <param name="request">Nuevo estado y motivo del cambio.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateStatus(
        [FromRoute] int id,
        [FromBody] UpdateDocumentStatusRequest request,
        CancellationToken ct)
    {
        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _generationService.UpdateStatusAsync(id, request, updatedBy, ct);
        return NoContent();
    }
}

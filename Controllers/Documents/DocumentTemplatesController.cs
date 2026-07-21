// ============================================================
// WsUtaSystem.Controllers.Documents.DocumentTemplatesController
// Motor Documental Institucional — Gestión de Plantillas HTML
// ============================================================
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Documents.Templates;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.Documents;

/// <summary>
/// Expone los endpoints REST para la gestión del ciclo de vida completo
/// de las plantillas documentales institucionales (HTML con tokens {{CAMPO}}).
///
/// Rutas base: <c>api/v1/documents/templates</c>
/// </summary>
[ApiController]
[Route("templates")]
public sealed class DocumentTemplatesController : ControllerBase
{
    private readonly IDocumentTemplateService _templateService;
    private readonly IDocumentTemplateFieldService _fieldService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DocumentTemplatesController> _logger;

    public DocumentTemplatesController(
        IDocumentTemplateService templateService,
        IDocumentTemplateFieldService fieldService,
        ICurrentUserService currentUser,
        ILogger<DocumentTemplatesController> logger)
    {
        _templateService = templateService;
        _fieldService    = fieldService;
        _currentUser     = currentUser;
        _logger          = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PLANTILLAS
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene la lista de plantillas con filtros opcionales por tipo y estado.
    /// </summary>
    /// <param name="templateType">Tipo de plantilla (ej: NOMBRAMIENTO, LICENCIA).</param>
    /// <param name="status">Estado de la plantilla (Draft, Published, Archived).</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet]
    [RequirePermission("DOCUMENTS.READ")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentTemplateSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? templateType,
        [FromQuery] DocumentTemplateStatus? status,
        CancellationToken ct)
    {
        var result = await _templateService.GetAllAsync(templateType, status, ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el detalle completo de una plantilla, incluyendo sus campos.
    /// </summary>
    /// <param name="id">Identificador de la plantilla.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("{id:int}")]
    [RequirePermission("DOCUMENTS.READ")]
    [ProducesResponseType(typeof(DocumentTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var template = await _templateService.GetDetailByIdAsync(id, ct);
        return template is null ? NotFound() : Ok(template);
    }

    /// <summary>
    /// Crea una nueva plantilla documental.
    /// </summary>
    /// <param name="request">Datos de la nueva plantilla.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost]
    [RequirePermission("DOCUMENTS.CREATE")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentTemplateRequest request,
        CancellationToken ct)
    {
        var createdBy = _currentUser.EmployeeId ?? 0;
        var id = await _templateService.CreateAsync(request, createdBy, ct);
        _logger.LogInformation("Plantilla documental creada. Id={TemplateId} por EmployeeId={UserId}", id, createdBy);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>
    /// Actualiza los metadatos de una plantilla existente.
    /// No modifica los campos; use el endpoint de campos para ello.
    /// </summary>
    /// <param name="id">Identificador de la plantilla.</param>
    /// <param name="request">Datos actualizados.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPut("{id:int}")]
    [RequirePermission("DOCUMENTS.UPDATE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdateDocumentTemplateRequest request,
        CancellationToken ct)
    {
        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _templateService.UpdateAsync(id, request, updatedBy, ct);
        return NoContent();
    }

    /// <summary>
    /// Cambia el estado de una plantilla (Draft → Published → Archived).
    /// Solo se permiten transiciones válidas según el flujo de estados.
    /// </summary>
    /// <param name="id">Identificador de la plantilla.</param>
    /// <param name="request">Nuevo estado y motivo del cambio.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPatch("{id:int}/status")]
    [RequirePermission("DOCUMENTS.UPDATE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] int id,
        [FromBody] ChangeTemplateStatusRequest request,
        CancellationToken ct)
    {
        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _templateService.ChangeStatusAsync(id, request, updatedBy, ct);
        _logger.LogInformation("Estado de plantilla Id={TemplateId} cambiado a {Status} por EmployeeId={UserId}",
            id, request.Status, updatedBy);
        return NoContent();
    }

    /// <summary>
    /// Genera una previsualización HTML del documento con datos reales o de muestra.
    /// Útil para validar el diseño antes de publicar la plantilla.
    /// </summary>
    /// <param name="request">Contexto de previsualización (entityId, overrides).</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("preview")]
    [RequirePermission("DOCUMENTS.READ")]
    [ProducesResponseType(typeof(PreviewTemplateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Preview(
        [FromBody] PreviewTemplateRequest request,
        CancellationToken ct)
    {
        var result = await _templateService.PreviewAsync(request, ct);
        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CAMPOS DE PLANTILLA
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene todos los campos definidos para una plantilla.
    /// </summary>
    /// <param name="id">Identificador de la plantilla.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("{id:int}/fields")]
    [RequirePermission("DOCUMENTS.READ")]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentTemplateFieldDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFields([FromRoute] int id, CancellationToken ct)
    {
        var fields = await _fieldService.GetByTemplateIdAsync(id, ct);
        return Ok(fields);
    }

    /// <summary>
    /// Agrega un nuevo campo a una plantilla existente.
    /// </summary>
    /// <param name="id">Identificador de la plantilla.</param>
    /// <param name="request">Datos del nuevo campo.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPost("{id:int}/fields")]
    [RequirePermission("DOCUMENTS.CREATE")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddField(
        [FromRoute] int id,
        [FromBody] CreateDocumentTemplateFieldRequest request,
        CancellationToken ct)
    {
        var createdBy = _currentUser.EmployeeId ?? 0;
        var fieldId = await _fieldService.CreateAsync(id, request, createdBy, ct);
        return CreatedAtAction(nameof(GetFields), new { id }, new { fieldId });
    }

    /// <summary>
    /// Actualiza la configuración de un campo de plantilla.
    /// </summary>
    /// <param name="id">Identificador de la plantilla (para contexto de ruta).</param>
    /// <param name="fieldId">Identificador del campo.</param>
    /// <param name="request">Datos actualizados del campo.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpPut("{id:int}/fields/{fieldId:int}")]
    [RequirePermission("DOCUMENTS.UPDATE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateField(
        [FromRoute] int id,
        [FromRoute] int fieldId,
        [FromBody] UpdateDocumentTemplateFieldRequest request,
        CancellationToken ct)
    {
        var updatedBy = _currentUser.EmployeeId ?? 0;
        await _fieldService.UpdateAsync(fieldId, request, updatedBy, ct);
        return NoContent();
    }

    /// <summary>
    /// Elimina un campo de una plantilla.
    /// Solo se permite si la plantilla está en estado Draft.
    /// </summary>
    /// <param name="id">Identificador de la plantilla (para contexto de ruta).</param>
    /// <param name="fieldId">Identificador del campo a eliminar.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpDelete("{id:int}/fields/{fieldId:int}")]
    [RequirePermission("DOCUMENTS.DELETE")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteField(
        [FromRoute] int id,
        [FromRoute] int fieldId,
        CancellationToken ct)
    {
        await _fieldService.DeleteAsync(fieldId, ct);
        return NoContent();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // VERSIONAMIENTO
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea una nueva versión de una plantilla existente.
    /// La nueva versión copia el contenido y campos del origen y queda en estado Draft.
    /// </summary>
    [HttpPost("{id:int}/version")]
    [RequirePermission("DOCUMENTS.CREATE")]
    [ProducesResponseType(typeof(CreateVersionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateVersion(
        [FromRoute] int id,
        [FromBody] CreateVersionRequest request,
        CancellationToken ct)
    {
        var createdBy = _currentUser.EmployeeId ?? 0;
        var result = await _templateService.CreateVersionAsync(id, request.Version, createdBy, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.NewTemplateId }, result);
    }

    /// <summary>
    /// Obtiene el historial de versiones de todas las plantillas con el mismo código.
    /// </summary>
    [HttpGet("code/{code}/versions")]
    [RequirePermission("DOCUMENTS.READ")]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateVersionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersionsByCode([FromRoute] string code, CancellationToken ct)
    {
        var result = await _templateService.GetVersionsByCodeAsync(code, ct);
        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IMPORTACIÓN DE TEXTO DE CONTRATO
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene los tipos de contrato relevantes para una plantilla (misma familia documental
    /// o que la tienen configurada como plantilla por defecto), para alimentar el selector
    /// del modal de importación de texto de contrato.
    /// </summary>
    /// <param name="id">Identificador de la plantilla.</param>
    /// <param name="ct">Token de cancelación.</param>
    [HttpGet("{id:int}/contract-types")]
    [RequirePermission("DOCUMENTS.READ")]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateContractTypeOptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContractTypesForTemplate([FromRoute] int id, CancellationToken ct)
    {
        var result = await _templateService.GetContractTypesForTemplateAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene los tipos de acción de personal relevantes para una plantilla, para alimentar
    /// el diálogo "Usar esta versión" en el editor.
    /// </summary>
    [HttpGet("{id:int}/action-types")]
    [RequirePermission("DOCUMENTS.READ")]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateActionTypeOptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActionTypesForTemplate([FromRoute] int id, CancellationToken ct)
    {
        var result = await _templateService.GetActionTypesForTemplateAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Analiza el ContractText de un tipo de contrato y retorna el texto original
    /// junto con los placeholders legados {N} detectados para facilitar la migración.
    /// </summary>
    [HttpGet("contract-types/{contractTypeId:int}/import-text")]
    [RequirePermission("DOCUMENTS.READ")]
    [ProducesResponseType(typeof(ImportContractTextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportContractText([FromRoute] int contractTypeId, CancellationToken ct)
    {
        var result = await _templateService.ImportContractTextAsync(contractTypeId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Extrae los tokens {{CAMPO}} de un fragmento HTML arbitrario.
    /// Útil para detectar los campos que necesita una plantilla antes de guardarla.
    /// </summary>
    [HttpPost("extract-tokens")]
    [RequirePermission("DOCUMENTS.READ")]
    [ProducesResponseType(typeof(ExtractTokensResponse), StatusCodes.Status200OK)]
    public IActionResult ExtractTokens([FromBody] ExtractTokensRequest request)
    {
        var result = _templateService.ExtractTokens(request);
        return Ok(result);
    }
}

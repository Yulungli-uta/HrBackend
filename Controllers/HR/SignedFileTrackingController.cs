using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.SignedFileTracking;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Acciones de personal y contratos que tienen archivo firmado registrado, con vista previa del archivo.
/// Solo lectura. Cada endpoint usa el permiso READ del módulo correspondiente y respeta el alcance por departamento.
/// </summary>
[ApiController]
[Route("signed-file-tracking")]
public sealed class SignedFileTrackingController : ControllerBase
{
    private readonly ISignedFileTrackingService _service;
    private readonly ICurrentUserService _currentUser;

    public SignedFileTrackingController(ISignedFileTrackingService service, ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    /// <summary>Lista paginada de acciones de personal con archivo firmado.</summary>
    [HttpGet("personnel-actions")]
    [RequirePermission("PERSONNEL_ACTIONS.READ")]
    public async Task<IActionResult> GetActions(
        [FromQuery] string? search = null,
        [FromQuery] int? actionTypeId = null,
        [FromQuery] string? status = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new SignedFileTrackingFilter(search, actionTypeId, status, null, departmentId, startDate, endDate, page, pageSize);
        return Ok(await _service.GetActionsAsync(filter, _currentUser.EmployeeId, ct));
    }

    /// <summary>Vista previa (PDF) del archivo firmado de una acción de personal.</summary>
    [HttpGet("personnel-actions/{actionId:int}/file")]
    [RequirePermission("PERSONNEL_ACTIONS.READ")]
    public async Task<IActionResult> PreviewActionFile([FromRoute] int actionId, CancellationToken ct) =>
        ToFileResult(await _service.PreviewActionFileAsync(actionId, _currentUser.EmployeeId, ct));

    /// <summary>Lista paginada de contratos con archivo firmado.</summary>
    [HttpGet("contracts")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> GetContracts(
        [FromQuery] string? search = null,
        [FromQuery] int? contractTypeId = null,
        [FromQuery] int? statusTypeId = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var filter = new SignedFileTrackingFilter(search, contractTypeId, null, statusTypeId, departmentId, startDate, endDate, page, pageSize);
        return Ok(await _service.GetContractsAsync(filter, _currentUser.EmployeeId, ct));
    }

    /// <summary>Vista previa (PDF) del archivo firmado de un contrato.</summary>
    [HttpGet("contracts/{contractId:int}/file")]
    [RequirePermission("CONTRACTS.READ")]
    public async Task<IActionResult> PreviewContractFile([FromRoute] int contractId, CancellationToken ct) =>
        ToFileResult(await _service.PreviewContractFileAsync(contractId, _currentUser.EmployeeId, ct));

    private IActionResult ToFileResult(SignedFilePreviewResult result)
    {
        switch (result.Status)
        {
            case SignedFilePreviewStatus.Ok:
                var disposition = new ContentDisposition { FileName = result.FileName ?? "documento.pdf", Inline = true };
                Response.Headers.ContentDisposition = disposition.ToString();
                Response.Headers.XContentTypeOptions = "nosniff";
                return File(result.Content!, result.ContentType ?? MediaTypeNames.Application.Pdf);
            case SignedFilePreviewStatus.FileMissing:
                return NotFound(new { code = "FILE_MISSING", message = "El registro tiene un archivo enlazado, pero no se encuentra en el servidor de archivos." });
            case SignedFilePreviewStatus.StoreUnavailable:
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { code = "FILE_STORE_UNAVAILABLE", message = "No se pudo acceder al servidor de archivos. Intenta nuevamente en unos minutos." });
            default:
                return NotFound(new { code = "RECORD_NOT_FOUND", message = "El registro no existe, no tiene archivo firmado o no tienes acceso a su departamento." });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Publications;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Revisión de publicaciones sincronizadas por la Dirección de Investigación (permiso PUBLICATIONS.APPROVE).
/// Aprobar congela la publicación; rechazar y reabrir exigen un motivo.
/// </summary>
[ApiController]
[Route("cv/publications/review")]
[RequirePermission("PUBLICATIONS.APPROVE")]
public class PublicationReviewController : ControllerBase
{
    private readonly IPublicationReviewService _svc;
    private readonly ICurrentUserService _currentUser;

    public PublicationReviewController(IPublicationReviewService svc, ICurrentUserService currentUser)
    {
        _svc = svc;
        _currentUser = currentUser;
    }

    /// <summary>Lista paginada de publicaciones para revisar, con filtros.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage([FromQuery] PublicationReviewFilterDto filter, CancellationToken ct) =>
        Ok(await _svc.GetPageAsync(filter, ct));

    /// <summary>Totales por estado para las tarjetas de la pantalla.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct) => Ok(await _svc.GetSummaryAsync(ct));

    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody] PublicationReviewActionRequest request, CancellationToken ct) =>
        Ok(await _svc.ApproveAsync(request.Ids, _currentUser.EmployeeId, ct));

    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody] PublicationReviewActionRequest request, CancellationToken ct) =>
        Ok(await _svc.RejectAsync(request.Ids, request.Notes, _currentUser.EmployeeId, ct));

    [HttpPost("reopen")]
    public async Task<IActionResult> Reopen([FromBody] PublicationReviewActionRequest request, CancellationToken ct) =>
        Ok(await _svc.ReopenAsync(request.Ids, request.Notes, _currentUser.EmployeeId, ct));

    [HttpPost("restore")]
    public async Task<IActionResult> Restore([FromBody] PublicationReviewActionRequest request, CancellationToken ct) =>
        Ok(await _svc.RestoreAsync(request.Ids, _currentUser.EmployeeId, ct));
}

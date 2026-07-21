using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Academic;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Permite disparar manualmente los jobs programados desde el panel de administración.
/// Útil para re-procesar sin esperar la ejecución nocturna.
/// </summary>
[ApiController]
[Route("scheduled-jobs")]
public class ScheduledJobsController : ControllerBase
{
    private readonly IContractExpirationService _contractExpiration;
    private readonly IStudentEnrollmentSyncService _studentSync;
    private readonly IEmployeeProvisioningClient _provisioningClient;
    private readonly ILogger<ScheduledJobsController> _logger;

    public ScheduledJobsController(
        IContractExpirationService contractExpiration,
        IStudentEnrollmentSyncService studentSync,
        IEmployeeProvisioningClient provisioningClient,
        ILogger<ScheduledJobsController> logger)
    {
        _contractExpiration  = contractExpiration  ?? throw new ArgumentNullException(nameof(contractExpiration));
        _studentSync         = studentSync         ?? throw new ArgumentNullException(nameof(studentSync));
        _provisioningClient  = provisioningClient  ?? throw new ArgumentNullException(nameof(provisioningClient));
        _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ejecuta manualmente el proceso de contratos vencidos:
    /// detecta contratos VIGENTES con EndDate anterior a hoy sin addenda activa,
    /// los marca VENCIDO y deshabilita cuentas AD si RequiresAdUserDisable = true.
    /// </summary>
    [HttpPost("contract-expiration/run")]
    [RequirePermission("SCHEDULED_JOBS.MANAGE")]
    public async Task<IActionResult> RunContractExpiration(CancellationToken ct)
    {
        _logger.LogInformation(
            "Ejecución manual del proceso de contratos vencidos. Usuario={User}",
            User.Identity?.Name ?? "desconocido");

        var serviceToken = await _provisioningClient.GetServiceTokenAsync(ct) ?? string.Empty;

        var processed = await _contractExpiration.ProcessExpiredContractsAsync(serviceToken, ct);

        return Ok(new
        {
            success  = true,
            message  = processed == 0
                ? "No se encontraron contratos VIGENTES vencidos para procesar."
                : $"Proceso completado: {processed} contrato(s) procesado(s).",
            processed
        });
    }

    /// <summary>
    /// Ejecuta manualmente la sincronización de matrículas de estudiantes:
    /// aprovisiona cuentas AD para nuevos matriculados y deshabilita las de no re-matriculados.
    /// </summary>
    [HttpPost("student-enrollment/run")]
    [RequirePermission("SCHEDULED_JOBS.MANAGE")]
    public async Task<IActionResult> RunStudentEnrollmentSync(
        [FromQuery] string periodCode,
        [FromQuery] string? previousPeriod,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Ejecución manual sincronización matrícula. PeriodCode={Period} PreviousPeriod={Prev} Usuario={User}",
            periodCode, previousPeriod ?? "(ninguno)", User.Identity?.Name ?? "desconocido");

        if (string.IsNullOrWhiteSpace(periodCode))
            return BadRequest(new { success = false, message = "periodCode es requerido." });

        var serviceToken = await _provisioningClient.GetServiceTokenAsync(ct) ?? string.Empty;

        var provisioned = await _studentSync.SyncPeriodAsync(periodCode, serviceToken, ct);

        int disabled = 0;
        if (!string.IsNullOrWhiteSpace(previousPeriod))
            disabled = await _studentSync.DisableNonReEnrolledAsync(periodCode, previousPeriod, serviceToken, ct);

        return Ok(new
        {
            success    = true,
            message    = $"Sincronización completada: {provisioned} cuenta(s) aprovisionada(s), {disabled} deshabilitada(s).",
            provisioned,
            disabled
        });
    }
}

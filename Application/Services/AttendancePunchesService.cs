using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class AttendancePunchesService : Service<AttendancePunches, int>, IAttendancePunchesService
{
    private readonly ILogger<AttendancePunchesService> _logger;
    private readonly IAttendancePunchesRepository _repository;
    private readonly IEmailBuilder _emailBuilder;
    private readonly ICurrentUserService _currentUser;
    private readonly IPunchAccessPolicyService _accessPolicy;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> EmployeeGates = new();
    //private readonly EmailTemplatesOptions _emailTemplates;

    public AttendancePunchesService(IAttendancePunchesRepository repo, IEmailBuilder emailBuilder, ICurrentUserService currentUser,
        IPunchAccessPolicyService accessPolicy,
        //IOptions<EmailTemplatesOptions> emailTemplates,
        ILogger<AttendancePunchesService> logger) : base(repo) {        
        _repository = repo;
        _emailBuilder = emailBuilder;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
        //_emailTemplates = emailTemplates.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<AttendancePunches>> GetLastPunchAsync(int employeeId, CancellationToken ct)
    {
        return await _repository.GetLastPunchAsync(employeeId, ct);
    }

    public async Task<IEnumerable<AttendancePunches>> GetPunchesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        return await _repository.GetPunchesByDateRangeAsync(startDate, endDate,ct);
    }

    public async Task<IEnumerable<AttendancePunches>> GetPunchesByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        return await _repository.GetPunchesByEmployeeAsync(employeeId, startDate, endDate, ct);
    }

    public async Task<IEnumerable<AttendancePunches>> GetTodayPunchesByEmployeeAsync(int employeeId, CancellationToken ct)
    {
        return await _repository.GetTodayPunchesByEmployeeAsync(employeeId,ct);
        
    }
    public async Task<AttendancePunches> CreatePunchesWithIPAsync(AttendancePunches entity, string ipAddress, CancellationToken ct)
    {
        entity.PunchId = 0;
        //var _attendancePunches = attendancePunches;        
        entity.IpAddress = ipAddress;
        var created = await _repository.CreatePunchWithIPAsync(entity, ct);
        // 2) Notificar por email 
        //await TryNotifyPunchByEmailAsync(created, ct);
        await _emailBuilder.TryNotifyAsync(
            EmailTemplateKey.AttendancePunch,
            "Notificación de picada",
            generateEmailBody(created),
            to: _currentUser.Email,
            timeoutSeconds: 15,
            ct);
        return created;
    }

    /// <inheritdoc />
    public async Task<OwnPunchResult> RegisterOwnPunchAsync(AttendancePunches punch, CancellationToken ct)
    {
        var employeeId = _currentUser.EmployeeId;
        if (employeeId is null)
            return new OwnPunchResult(null, "Tu usuario no tiene un empleado asociado, por lo que no puede registrar picadas.", "NO_EMPLOYEE");

        // Solo se puede picar a nombre propio: el empleado del cuerpo debe coincidir con el del token.
        if (punch.EmployeeId != employeeId.Value)
        {
            _logger.LogWarning("Intento de picada a nombre de otro empleado: token={TokenEmployee} cuerpo={BodyEmployee}", employeeId, punch.EmployeeId);
            return new OwnPunchResult(null, "Solo puedes registrar tu propia picada.", "NOT_OWN_PUNCH");
        }

        // Una picada a la vez por empleado: dos clics simultáneos no pueden saltarse el intervalo mínimo.
        var gate = EmployeeGates.GetOrAdd(employeeId.Value, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            // La hora la define el servidor (el cliente no puede adelantarla ni atrasarla).
            var now = DateTime.Now;
            now = now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));

            // Intervalo mínimo entre picadas (el trigger de BD que lo hacía está deshabilitado a propósito).
            var minMinutes = await _accessPolicy.GetMinIntervalMinutesAsync(ct);
            var last = (await _repository.GetLastPunchAsync(employeeId.Value, ct)).FirstOrDefault();
            var remaining = PunchCooldown.Remaining(last?.PunchTime, now, minMinutes);
            if (remaining is not null)
                return new OwnPunchResult(null, PunchCooldown.Message(remaining.Value), "PUNCH_TOO_SOON");

            var decision = await _accessPolicy.EvaluateAsync(
                employeeId.Value, _currentUser.GetRemoteIp(), _currentUser.GetForwardedForHeader(), ct);
            if (!decision.Allowed)
                return new OwnPunchResult(null, decision.Message, "PUNCH_OUTSIDE_CAMPUS");

            punch.PunchTime = now;
            var created = await CreatePunchesWithIPAsync(punch, _currentUser.GetIp() ?? string.Empty, ct);
            return new OwnPunchResult(created, null, null);
        }
        finally
        {
            gate.Release();
        }
    }

    private string generateEmailBody(AttendancePunches punch)
    {
        var when = punch.PunchTime;
        var ip = punch.IpAddress ?? "N/A";
        var body =
            $"<p> Se ha registrado una picada.</p>" +
            $"<ul>" +
            $"<li><b>Empleado:</b> {punch.EmployeeId}</li>" +
            $"<li><b>Fecha/Hora:</b> {when:yyyy-MM-dd HH:mm:ss}</li>" +
            $"<li><b>IP:</b> {ip}</li>" +
            $"</ul>";
        return body;
    }

    //private async Task TryNotifyPunchByEmailAsync(AttendancePunches punch, CancellationToken ct)
    //{
    //    try
    //    {
    //        var to = _currentUser.Email;

    //        if (string.IsNullOrWhiteSpace(to))
    //        {
    //            _logger.LogWarning("No se envió correo de picada: ICurrentUserService.Email es null/vacío.");
    //            return;
    //        }

    //        // Si no existe, EmailSenderService enviará sin layout (degrada).
    //        //const string layoutSlug = "informativo";
    //        // Resolver slug por clave (enum)
    //        var templateKey = EmailTemplateKey.AttendancePunch;
    //        if (!_emailTemplates.Layouts.TryGetValue(templateKey.ToString(), out var layoutSlug))
    //        {
    //            _logger.LogWarning(
    //                "No existe layout configurado para EmailTemplates:Layouts:{Key}",
    //                templateKey);
    //            return;
    //        }

    //        var when = punch.PunchTime;
    //        var ip = punch.IpAddress ?? "N/A";

    //        var body =
    //            $"<p>Se ha registrado una picada.</p>" +
    //            $"<ul>" +
    //            $"<li><b>Empleado:</b> {punch.EmployeeId}</li>" +
    //            $"<li><b>Fecha/Hora:</b> {when:yyyy-MM-dd HH:mm:ss}</li>" +
    //            $"<li><b>IP:</b> {ip}</li>" +
    //            $"</ul>";

    //        // Token aislado del request HTTP (timeout propio)
    //        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    //        var emailCt = timeoutCts.Token;

    //        //var res = await _emailBuilder
    //        //    .To(to.Trim())
    //        //    .Subject("Notificación de picada")
    //        //    .WithLayout(layoutSlug) // <- usa el slug leído desde appsettings
    //        //    .WithBody(body)
    //        //    .CreatedBy(_currentUser.EmployeeId)
    //        //    .SendAsync(emailCt);
    //        await _emailBuilder
    //        .To(to.Trim())
    //        .Subject("Notificación de picada")
    //        .WithLayout(layoutSlug)
    //        .WithBody(body)
    //        .CreatedBy(_currentUser.EmployeeId)
    //        .QueueAsync();

    //        //if (!res.Success)
    //        //{
    //        //    _logger.LogWarning(
    //        //        "Correo de picada no enviado (registrado en EmailLogs). EmailLogID={EmailLogID} Msg={Msg}",
    //        //        res.EmailLogID, res.Message);
    //        //}
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error inesperado enviando correo de notificación de picada (best-effort).");
    //    }
    //}
}

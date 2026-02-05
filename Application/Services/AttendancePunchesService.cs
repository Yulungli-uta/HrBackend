using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
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
    //private readonly EmailTemplatesOptions _emailTemplates;

    public AttendancePunchesService(IAttendancePunchesRepository repo, IEmailBuilder emailBuilder, ICurrentUserService currentUser,
        //IOptions<EmailTemplatesOptions> emailTemplates,
        ILogger<AttendancePunchesService> logger) : base(repo) {        
        _repository = repo;
        _emailBuilder = emailBuilder;
        _currentUser = currentUser;
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

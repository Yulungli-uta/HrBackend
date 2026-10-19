using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Application.Interfaces.Services;
/// <summary>Resultado de registrar la propia picada: creada, o rechazada con un mensaje para el usuario.</summary>
public sealed record OwnPunchResult(AttendancePunches? Punch, string? DeniedMessage, string? DeniedCode)
{
    public bool Succeeded => Punch is not null;
}

public interface IAttendancePunchesService : IService<AttendancePunches, int>
{
    Task<IEnumerable<AttendancePunches>> GetLastPunchAsync(int employeeId, CancellationToken ct);
    Task<IEnumerable<AttendancePunches>> GetTodayPunchesByEmployeeAsync(int employeeId, CancellationToken ct);
    Task<IEnumerable<AttendancePunches>> GetPunchesByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct);
    Task<IEnumerable<AttendancePunches>> GetPunchesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct);
    Task<AttendancePunches> CreatePunchesWithIPAsync(AttendancePunches attendancePunches, String ipAddress, CancellationToken ct);

    /// <summary>
    /// Registra la picada del empleado autenticado: el empleado sale del token (no del cuerpo), la hora la pone el
    /// servidor y se aplica el control de red (campus / picada externa autorizada). Devuelve la picada creada o el
    /// motivo por el que se rechazó.
    /// </summary>
    Task<OwnPunchResult> RegisterOwnPunchAsync(AttendancePunches punch, CancellationToken ct);
}

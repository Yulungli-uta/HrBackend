using WsUtaSystem.Application.DTOs.EmployeeSelfService;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>
/// Lectura agregada para el panel de autoservicio (<c>GET /employee-self-service/summary</c>).
/// Reemplaza ~8 llamadas secuenciales a Permissions/Vacations/Certificates/InternalRequests/
/// AttendancePunches/Justifications por una sola ida y vuelta a SQL Server
/// (<c>HR.sp_GetEmployeeSelfServiceSummary</c>, ver Database/hr/11_employee_self_service.sql).
/// Dapper puro (no EF) — igual que <see cref="IHrBalanceRepository"/> — porque es una
/// consulta compuesta de solo lectura, exactamente el caso que HrBackend/CLAUDE.md reserva
/// para Dapper ("consultas complejas, reportes o SQL optimizado").
/// </summary>
public interface IEmployeeSelfServiceRepository
{
    Task<EmployeeSelfServiceSummaryRawData> GetSummaryDataAsync(int employeeId, CancellationToken ct = default);
}

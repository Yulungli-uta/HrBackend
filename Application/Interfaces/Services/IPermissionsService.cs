using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IPermissionsService : IService<Permissions, int> {

    Task<Permissions> CreateWithBalanceCheckAsync(Permissions entity, CancellationToken ct);
    Task<Permissions> UpdateBalanceAffectAsync(int id, Permissions entity, CancellationToken ct);
    Task<IEnumerable<Permissions>> GetByEmployeeId(int EmployeeId, CancellationToken ct);
    Task<IEnumerable<Permissions>> GetByImmediateBossId(int immediateBossId, CancellationToken ct);
    Task<IEnumerable<Permissions>> GetByImmediateBossIdNonMedical(int employeeId, CancellationToken ct);
    Task<IEnumerable<Permissions>> GetPendingMedicalPermissions(CancellationToken ct);

    /// <summary>
    /// Retorna permisos otorgados para reporte con filtros de rango de fechas, estado y empleado.
    /// Incluye tipo de permiso, dependencia del empleado y nombre de quien aprobó.
    /// </summary>
    Task<IReadOnlyList<PermissionReportDto>> GetForReportAsync(ReportFilterDto filter, CancellationToken ct = default);
}


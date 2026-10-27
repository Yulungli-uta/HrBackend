using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>
/// Alcance por empleado de los reportes de asistencia: dependencia y régimen laboral.
/// </summary>
/// <remarks>
/// <para>
/// Primero se resuelven los empleados que cumplen esos filtros (consulta pequeña: ~1.500 empleados) y recién
/// entonces se filtran los cálculos de asistencia por esos IDs. Aplicar el filtro fila por fila sobre
/// <c>tbl_AttendanceCalculations</c>, uniéndolo con la vista de empleados y con subconsultas correlacionadas,
/// hacía que SQL Server reutilizara un plan armado para el primer valor consultado: una dependencia pequeña
/// dejaba un plan que con una grande tardaba más de un minuto (medido 2026-10-27: 0,06 s frente a 67 s).
/// Resolver primero a los empleados da planes estables sin necesidad de hints.
/// </para>
/// <para>
/// El resultado es idéntico al de la consulta anterior: se aplica el mismo predicado sobre la misma fila de
/// empleado, y la unión con la asistencia conserva todas las filas.
/// </para>
/// <para>
/// Régimen efectivo del empleado: prioriza <c>EmployeeLaborRegime</c> activo; si el empleado no tiene ninguno,
/// cae a <c>EmployeeType</c> legacy en vez de excluirlo en silencio (mismo criterio que el subsidio de
/// alimentación). Este criterio se escribe solo en los dos métodos de este archivo, uno por cada fuente de
/// empleado (tabla y vista); EF Core exige la expresión en línea para poder traducirla a SQL.
/// </para>
/// </remarks>
public sealed partial class AttendanceCalculationsReportRepository
{
    /// <summary>
    /// Empleados (<c>HR.tbl_Employees</c>) que cumplen dependencia y régimen. <c>null</c> = ninguno de los dos
    /// filtros está activo (no se restringe por empleado). Lista vacía = nadie cumple.
    /// </summary>
    private async Task<List<int>?> ResolveEmployeeScopeAsync(ReportFilterDto filter, CancellationToken ct)
    {
        if (filter.DepartmentId is not > 0 && filter.LaborRegimeId is not > 0) return null;

        var query = _db.Employees.AsNoTracking().AsQueryable();

        if (filter.DepartmentId is > 0)
        {
            var departmentId = filter.DepartmentId.Value;
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        if (filter.LaborRegimeId is > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(e =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == e.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == e.EmployeeId && r.IsActive)
                    && e.EmployeeType == regimeId));
        }

        return await query.Select(e => e.EmployeeId).Distinct().ToListAsync(ct);
    }

    /// <summary>
    /// Igual que <see cref="ResolveEmployeeScopeAsync"/> pero sobre <c>HR.vw_EmployeeDetails</c>, que es la fila de
    /// empleado que usan las consultas basadas en esa vista.
    /// </summary>
    private async Task<List<int>?> ResolveEmployeeScopeFromViewAsync(ReportFilterDto filter, CancellationToken ct)
    {
        if (filter.DepartmentId is not > 0 && filter.LaborRegimeId is not > 0) return null;

        var query = _db.vwEmployeeDetails.AsNoTracking().AsQueryable();

        if (filter.DepartmentId is > 0)
        {
            var departmentId = filter.DepartmentId.Value;
            query = query.Where(v => v.DepartmentID == departmentId);
        }

        if (filter.LaborRegimeId is > 0)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(v =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == v.EmployeeID && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == v.EmployeeID && r.IsActive)
                    && v.EmployeeType == regimeId));
        }

        return await query.Select(v => v.EmployeeID).Distinct().ToListAsync(ct);
    }
}

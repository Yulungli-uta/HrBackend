using WsUtaSystem.Application.DTOs.TeacherStructure;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Consultas de solo lectura del listado de docentes titulares (estructura docente + libro de sueldos + nombramiento).</summary>
public interface ITeacherRosterRepository
{
    /// <summary>
    /// Docentes titulares con estructura docente activa. <paramref name="page"/> / <paramref name="pageSize"/> nulos
    /// devuelven todas las filas (uso del reporte). El RMU solo se consulta si <paramref name="includeSalary"/> es true.
    /// </summary>
    Task<IReadOnlyList<TeacherRosterRow>> GetRowsAsync(
        TeacherRosterQuery query, bool includeSalary, int? page, int? pageSize, CancellationToken ct);

    /// <summary>
    /// Conteos agrupados por categoría, nivel, dedicación y estado del empleado. Aplica todos los filtros de
    /// <paramref name="query"/> salvo los que indique <paramref name="ignore"/> (el del propio grupo de tarjetas).
    /// </summary>
    Task<IReadOnlyList<TeacherRosterGroupRow>> GetGroupsAsync(
        TeacherRosterQuery query, TeacherRosterIgnore ignore, CancellationToken ct);

    /// <summary>Empleados activos con una acción de escalafón firmada y sin estructura docente activa.</summary>
    Task<int> CountActiveWithScaleActionWithoutStructureAsync(CancellationToken ct);
}

using WsUtaSystem.Application.DTOs.TeacherStructure;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Consultas de solo lectura del listado de docentes no titulares (docentes ocasionales).</summary>
public interface INonTitularRosterRepository
{
    /// <summary>
    /// Página de docentes no titulares; <paramref name="page"/> / <paramref name="pageSize"/> nulos devuelven todas las filas
    /// (uso del reporte). El RMU solo se consulta si <paramref name="includeSalary"/> es true.
    /// </summary>
    Task<IReadOnlyList<NonTitularRosterRow>> GetRowsAsync(
        NonTitularRosterQuery query, bool includeSalary, int? page, int? pageSize, CancellationToken ct);

    /// <summary>
    /// Conteos agrupados por dedicación, tipo de contrato y estado. Aplica todos los filtros de
    /// <paramref name="query"/> salvo los que indique <paramref name="ignore"/> (el del propio grupo de tarjetas).
    /// </summary>
    Task<IReadOnlyList<NonTitularRosterGroupRow>> GetGroupsAsync(
        NonTitularRosterQuery query, NonTitularRosterIgnore ignore, CancellationToken ct);
}

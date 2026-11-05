using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TeacherStructure;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Listado y conteos de docentes titulares (página "Personal Docente Titular" y su reporte).</summary>
public interface ITeacherRosterService
{
    /// <summary>
    /// Página de docentes titulares con estructura docente activa. El RMU solo se incluye si
    /// <paramref name="canViewSalary"/> es true (permiso SALARY_HISTORY.READ verificado en el servidor).
    /// </summary>
    Task<PagedResult<TeacherRosterItemDto>> GetPagedAsync(TeacherRosterFilterDto filter, bool canViewSalary, CancellationToken ct);

    /// <summary>Todas las filas que cumplen el filtro, sin paginar (uso del reporte exportable).</summary>
    Task<IReadOnlyList<TeacherRosterItemDto>> GetAllAsync(TeacherRosterFilterDto filter, bool canViewSalary, CancellationToken ct);

    /// <summary>Conteos para las tarjetas: total, por categoría, por nivel, por dedicación y categoría × dedicación.</summary>
    Task<TeacherRosterSummaryDto> GetSummaryAsync(TeacherRosterFilterDto filter, CancellationToken ct);
}

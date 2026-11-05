using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TeacherStructure;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Listado y conteos de docentes no titulares (pestaña "No titulares" de Personal Docente).</summary>
public interface INonTitularRosterService
{
    /// <summary>
    /// Página de docentes no titulares. El RMU solo se incluye si <paramref name="canViewSalary"/> es true
    /// (permiso SALARY_HISTORY.READ verificado en el servidor).
    /// </summary>
    Task<PagedResult<NonTitularRosterItemDto>> GetPagedAsync(NonTitularRosterFilterDto filter, bool canViewSalary, CancellationToken ct);

    /// <summary>Todas las filas que cumplen el filtro, sin paginar (uso del reporte exportable).</summary>
    Task<IReadOnlyList<NonTitularRosterItemDto>> GetAllAsync(NonTitularRosterFilterDto filter, bool canViewSalary, CancellationToken ct);

    /// <summary>Conteos para las tarjetas: total, por dedicación y por tipo de contrato, con los filtros de la pestaña.</summary>
    Task<NonTitularRosterSummaryDto> GetSummaryAsync(NonTitularRosterFilterDto filter, CancellationToken ct);
}

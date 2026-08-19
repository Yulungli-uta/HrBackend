using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.FamilyBurden;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IFamilyBurdenRepository : IRepository<FamilyBurden, int>
{
    Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId);

    /// <summary>Listado para la pantalla de validación, con nombre de empleado y catálogos resueltos.</summary>
    /// <param name="search">Filtro opcional por cédula o nombre/apellido del empleado titular.</param>
    Task<PagedResult<FamilyBurdenValidationListItemDto>> GetForValidationAsync(
        int? statusTypeId, string? search, int page, int pageSize, CancellationToken ct);

    /// <summary>Contadores agregados (total, por estado, con discapacidad) para dato gerencial.</summary>
    Task<FamilyBurdenStatsDto> GetStatsAsync(CancellationToken ct);
}

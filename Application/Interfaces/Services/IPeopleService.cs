using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IPeopleService : IService<People, int>
{
    /// <summary>Retorna las personas cuyo IdCard coincida con alguna de las identificaciones dadas.</summary>
    Task<List<People>> GetByIdentificationsAsync(IEnumerable<string> identifications, CancellationToken ct);

    /// <summary>
    /// Búsqueda paginada por palabra: cada palabra escrita debe aparecer en algún
    /// campo (nombre, apellido, cédula o email), sin importar el orden en que se
    /// escriban. Ordenado por Apellido.
    /// </summary>
    Task<PagedResult<People>> SearchPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
}

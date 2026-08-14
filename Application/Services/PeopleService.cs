using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PeopleService : Service<People, int>, IPeopleService
{
    public PeopleService(IPeopleRepository repo) : base(repo) { }

    /// <inheritdoc/>
    public async Task<PagedResult<People>> SearchPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _repo.Query();

        // Búsqueda por palabra: cada palabra escrita (ej. "Perez Juan") debe
        // aparecer en ALGUNO de los campos, no las dos juntas en un solo campo —
        // antes comparaba la frase completa contra cada campo por separado, lo
        // que nunca coincidía si el usuario escribía apellido y nombre juntos.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var words = search.Trim().ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                var w = word;
                query = query.Where(p =>
                    (p.FirstName != null && p.FirstName.ToLower().Contains(w)) ||
                    (p.LastName != null && p.LastName.ToLower().Contains(w)) ||
                    (p.IdCard != null && p.IdCard.ToLower().Contains(w)) ||
                    (p.Email != null && p.Email.ToLower().Contains(w)));
            }
        }

        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<People>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public async Task<List<People>> GetByIdentificationsAsync(
        IEnumerable<string> identifications,
        CancellationToken ct)
    {
        var normalized = identifications
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim().ToLower())
            .Distinct()
            .ToList();

        return await _repo.Query()
            .Where(p => p.IdCard != null && normalized.Contains(p.IdCard.ToLower()))
            .ToListAsync(ct);
    }
}

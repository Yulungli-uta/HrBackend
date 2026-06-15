using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PeopleService : Service<People, int>, IPeopleService
{
    public PeopleService(IPeopleRepository repo) : base(repo) { }

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

using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class ResearcherProfilesRepository : IResearcherProfilesRepository
{
    private readonly AppDbContext _db;

    public ResearcherProfilesRepository(AppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task<ResearcherProfile?> GetByPersonIdAsync(int personId, CancellationToken ct) =>
        _db.ResearcherProfiles.Include(p => p.ProviderIds).FirstOrDefaultAsync(p => p.PersonId == personId, ct);

    public async Task<(string FirstName, string LastName)?> GetPersonNameAsync(int personId, CancellationToken ct)
    {
        var row = await _db.People.AsNoTracking()
            .Where(p => p.PersonId == personId)
            .Select(p => new { p.FirstName, p.LastName })
            .FirstOrDefaultAsync(ct);
        return row is null ? null : (row.FirstName, row.LastName);
    }

    public Task<bool> OrcidExistsForOtherPersonAsync(string orcid, int personId, CancellationToken ct) =>
        _db.ResearcherProfiles.AsNoTracking().AnyAsync(p => p.Orcid == orcid && p.PersonId != personId, ct);

    public Task<bool> ActiveProviderIdOwnedByOtherAsync(int providerTypeId, string externalId, int profileId, CancellationToken ct) =>
        _db.ResearcherProviderIds.AsNoTracking().AnyAsync(
            x => x.ProviderTypeId == providerTypeId && x.ExternalId == externalId && x.IsActive && x.ResearcherProfileId != profileId, ct);

    public async Task AddProfileAsync(ResearcherProfile profile, CancellationToken ct)
    {
        _db.ResearcherProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddProviderIdAsync(ResearcherProviderId providerId, CancellationToken ct)
    {
        _db.ResearcherProviderIds.Add(providerId);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

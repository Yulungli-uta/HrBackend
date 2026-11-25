using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class OccupationalGroupRepository : ServiceAwareEfRepository<OccupationalGroup, int>, IOccupationalGroupRepository
{
    private const string FamilyCategory = "OCCUPATIONAL_GROUP_FAMILY";

    public OccupationalGroupRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    /// <inheritdoc/>
    public Task<bool> FamilyTypeExistsAsync(int familyTypeId, CancellationToken ct) =>
        _db.Set<RefTypes>().AsNoTracking().AnyAsync(t => t.TypeId == familyTypeId && t.Category == FamilyCategory && t.IsActive, ct);

    /// <inheritdoc/>
    public Task<bool> FamilyDegreeInUseAsync(int familyTypeId, int degreeId, int? excludeGroupId, CancellationToken ct) =>
        _set.AsNoTracking().AnyAsync(g => g.FamilyTypeId == familyTypeId && g.DegreeId == degreeId
                                          && (excludeGroupId == null || g.GroupId != excludeGroupId), ct);

    /// <inheritdoc/>
    public Task<bool> IsDegreeActiveAsync(int degreeId, CancellationToken ct) =>
        _db.Set<Degree>().AsNoTracking().AnyAsync(d => d.DegreeId == degreeId && d.IsActive, ct);

    /// <inheritdoc/>
    public Task<bool> FamilyLevelInUseAsync(int familyTypeId, int level, int? excludeGroupId, CancellationToken ct) =>
        _set.AsNoTracking().AnyAsync(g => g.FamilyTypeId == familyTypeId && g.Level == level
                                          && (excludeGroupId == null || g.GroupId != excludeGroupId), ct);

    /// <inheritdoc/>
    public async Task<(string Name, string? LevelPrefix)?> GetFamilyNamingAsync(int familyTypeId, CancellationToken ct)
    {
        var family = await _db.Set<RefTypes>().AsNoTracking()
            .Where(t => t.TypeId == familyTypeId && t.Category == FamilyCategory)
            .Select(t => new { t.Name, t.Metadata })
            .FirstOrDefaultAsync(ct);
        if (family is null) return null;

        string? prefix = null;
        if (!string.IsNullOrWhiteSpace(family.Metadata))
        {
            try
            {
                using var doc = JsonDocument.Parse(family.Metadata);
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("levelPrefix", out var p)
                    && p.ValueKind == JsonValueKind.String)
                    prefix = p.GetString();
            }
            catch (JsonException)
            {
                // Metadata sin formato JSON: se compone el nombre sin prefijo de nivel
            }
        }
        return (family.Name, prefix);
    }
}

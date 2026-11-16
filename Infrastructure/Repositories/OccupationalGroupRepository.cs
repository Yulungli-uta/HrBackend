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
}

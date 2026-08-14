using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.FamilyBurden;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace WsUtaSystem.Infrastructure.Repositories;
public class FamilyBurdenRepository : ServiceAwareEfRepository<FamilyBurden, int>, IFamilyBurdenRepository
{
    private readonly DbContext _db;
    public FamilyBurdenRepository(WsUtaSystem.Data.AppDbContext db) : base(db) {
        _db = db;
    }

    public async Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId)
    {
        return await _db.Set<FamilyBurden>().Where(f => f.PersonId == personId).ToListAsync();
    }

    public async Task<PagedResult<FamilyBurdenValidationListItemDto>> GetForValidationAsync(
        int? statusTypeId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from fb in _db.Set<FamilyBurden>().AsNoTracking()
            join emp in _db.Set<Employees>().AsNoTracking() on fb.PersonId equals emp.PersonID
            join person in _db.Set<People>().AsNoTracking() on fb.PersonId equals person.PersonId
            join status in _db.Set<RefTypes>().AsNoTracking() on fb.StatusTypeId equals status.TypeId into statusJoin
            from status in statusJoin.DefaultIfEmpty()
            join disability in _db.Set<RefTypes>().AsNoTracking() on fb.DisabilityTypeId equals disability.TypeId into disabilityJoin
            from disability in disabilityJoin.DefaultIfEmpty()
            join approver in _db.Set<Employees>().AsNoTracking() on fb.ApprovedBy equals (int?)approver.EmployeeId into approverEmpJoin
            from approverEmp in approverEmpJoin.DefaultIfEmpty()
            join approverPerson in _db.Set<People>().AsNoTracking() on approverEmp.PersonID equals approverPerson.PersonId into approverPersonJoin
            from approverPerson in approverPersonJoin.DefaultIfEmpty()
            join rejecter in _db.Set<Employees>().AsNoTracking() on fb.RejectedBy equals (int?)rejecter.EmployeeId into rejecterEmpJoin
            from rejecterEmp in rejecterEmpJoin.DefaultIfEmpty()
            join rejecterPerson in _db.Set<People>().AsNoTracking() on rejecterEmp.PersonID equals rejecterPerson.PersonId into rejecterPersonJoin
            from rejecterPerson in rejecterPersonJoin.DefaultIfEmpty()
            select new { fb, person, status, disability, approverPerson, rejecterPerson };

        if (statusTypeId.HasValue)
            query = query.Where(x => x.fb.StatusTypeId == statusTypeId.Value);

        var totalCount = await query.LongCountAsync(ct);

        var items = await query
            .OrderBy(x => x.person.LastName).ThenBy(x => x.person.FirstName).ThenByDescending(x => x.fb.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FamilyBurdenValidationListItemDto
            {
                BurdenId = x.fb.BurdenId,
                PersonId = x.fb.PersonId,
                EmployeeFullName = x.person.LastName + " " + x.person.FirstName,
                EmployeeIdCard = x.person.IdCard,
                DependentId = x.fb.DependentId,
                FirstName = x.fb.FirstName,
                LastName = x.fb.LastName,
                BirthDate = x.fb.BirthDate,
                DisabilityTypeId = x.fb.DisabilityTypeId,
                DisabilityTypeName = x.disability != null ? x.disability.Name : null,
                StatusTypeId = x.fb.StatusTypeId,
                StatusName = x.status != null ? x.status.Name : "REGISTRADO",
                CreatedAt = x.fb.CreatedAt,
                ApprovedAt = x.fb.ApprovedAt,
                ApprovedByName = x.approverPerson != null ? x.approverPerson.LastName + " " + x.approverPerson.FirstName : null,
                RejectedAt = x.fb.RejectedAt,
                RejectedByName = x.rejecterPerson != null ? x.rejecterPerson.LastName + " " + x.rejecterPerson.FirstName : null,
                RejectionReason = x.fb.RejectionReason
            })
            .ToListAsync(ct);

        return new PagedResult<FamilyBurdenValidationListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<FamilyBurdenStatsDto> GetStatsAsync(CancellationToken ct)
    {
        var statusIds = await _db.Set<RefTypes>()
            .Where(r => r.Category == "FAMILY_BURDEN_STATUS")
            .ToDictionaryAsync(r => r.Name, r => r.TypeId, ct);

        var all = await _db.Set<FamilyBurden>().AsNoTracking()
            .Select(fb => new { fb.StatusTypeId, fb.DisabilityTypeId })
            .ToListAsync(ct);

        statusIds.TryGetValue("REGISTRADO", out var registradoId);
        statusIds.TryGetValue("APROBADO", out var aprobadoId);
        statusIds.TryGetValue("RECHAZADO", out var rechazadoId);

        return new FamilyBurdenStatsDto
        {
            TotalCount = all.Count,
            RegisteredCount = all.Count(x => x.StatusTypeId == registradoId),
            ApprovedCount = all.Count(x => x.StatusTypeId == aprobadoId),
            RejectedCount = all.Count(x => x.StatusTypeId == rechazadoId),
            DisabilityCount = all.Count(x => x.DisabilityTypeId != null)
        };
    }
}

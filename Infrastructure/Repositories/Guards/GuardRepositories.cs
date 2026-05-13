using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Infrastructure.Repositories.Guards;

public class GuardServiceLocationRepository
    : ServiceAwareEfRepository<GuardServiceLocation, int>, IGuardServiceLocationRepository
{
    private readonly AppDbContext _db;
    public GuardServiceLocationRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<List<GuardServiceLocation>> GetTreeAsync(CancellationToken ct) =>
        await _db.GuardServiceLocations
            .Where(l => l.ParentLocationId == null && l.IsActive)
            .Include(l => l.Children.Where(c => c.IsActive))
                .ThenInclude(c => c.Children.Where(c2 => c2.IsActive))
                    .ThenInclude(c2 => c2.Children.Where(c3 => c3.IsActive))
            .OrderBy(l => l.LocationName)
            .ToListAsync(ct);

    public async Task<List<GuardServiceLocation>> GetAssignableAsync(CancellationToken ct) =>
        await _db.GuardServiceLocations
            .Where(l => l.IsAssignable && l.IsActive)
            .OrderBy(l => l.LocationName)
            .ToListAsync(ct);

    public async Task<List<GuardServiceLocation>> GetByRootAsync(int rootLocationId, CancellationToken ct) =>
        await _db.GuardServiceLocations
            .Where(l => l.RootLocationId == rootLocationId && l.IsActive)
            .OrderBy(l => l.Level).ThenBy(l => l.LocationName)
            .ToListAsync(ct);
}

public class GuardRotationGroupRepository
    : ServiceAwareEfRepository<GuardRotationGroup, int>, IGuardRotationGroupRepository
{
    private readonly AppDbContext _db;
    public GuardRotationGroupRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<GuardRotationGroup?> GetWithEmployeesAsync(int groupId, CancellationToken ct) =>
        await _db.GuardRotationGroups
            .Include(g => g.Employees.Where(e => e.IsActive))
                .ThenInclude(e => e.Employee)
                    .ThenInclude(e => e!.People)
            .FirstOrDefaultAsync(g => g.GroupId == groupId, ct);

    public async Task<List<GuardRotationGroupEmployee>> GetActiveEmployeesAsync(int groupId, DateOnly date, CancellationToken ct) =>
        await _db.GuardRotationGroupEmployees
            .Where(e => e.GroupId == groupId && e.IsActive
                && e.ValidFrom <= date && (e.ValidTo == null || e.ValidTo >= date))
            .Include(e => e.Employee).ThenInclude(e => e!.People)
            .ToListAsync(ct);
}

public class RotationPatternRepository
    : ServiceAwareEfRepository<RotationPattern, int>, IRotationPatternRepository
{
    private readonly AppDbContext _db;
    public RotationPatternRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<RotationPattern?> GetWithDetailsAsync(int patternId, CancellationToken ct) =>
        await _db.RotationPatterns
            .Include(p => p.Details.OrderBy(d => d.DayOrder))
                .ThenInclude(d => d.Schedule)
            .FirstOrDefaultAsync(p => p.PatternId == patternId, ct);
}

public class GuardShiftCoverageRequirementRepository
    : ServiceAwareEfRepository<GuardShiftCoverageRequirement, int>, IGuardShiftCoverageRequirementRepository
{
    private readonly AppDbContext _db;
    public GuardShiftCoverageRequirementRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<List<GuardShiftCoverageRequirement>> GetByLocationAsync(int locationId, DateOnly date, CancellationToken ct) =>
        await _db.GuardShiftCoverageRequirements
            .Where(r => r.LocationId == locationId && r.IsActive
                && r.ValidFrom <= date && (r.ValidTo == null || r.ValidTo >= date))
            .Include(r => r.Schedule)
            .ToListAsync(ct);
}

public class GuardShiftPlanningRepository
    : ServiceAwareEfRepository<GuardShiftPlanning, int>, IGuardShiftPlanningRepository
{
    private readonly AppDbContext _db;
    public GuardShiftPlanningRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<List<GuardShiftPlanning>> GetCalendarAsync(GuardShiftCalendarFilterDto filter, CancellationToken ct)
    {
        var q = _db.GuardShiftPlannings
            .Include(p => p.Employee).ThenInclude(e => e!.People)
            .Include(p => p.Location)
            .Include(p => p.Schedule)
            .Include(p => p.Group)
            .Include(p => p.Changes.Where(c => c.IsActiveForAttendance))
                .ThenInclude(c => c.ReplacementEmployee).ThenInclude(e => e!.People)
            .Where(p => p.WorkDate >= filter.StartDate && p.WorkDate <= filter.EndDate);

        if (filter.GroupId.HasValue)       q = q.Where(p => p.GroupId == filter.GroupId);
        if (filter.LocationId.HasValue)    q = q.Where(p => p.LocationId == filter.LocationId);
        if (filter.RootLocationId.HasValue)
            q = q.Where(p => p.Location!.RootLocationId == filter.RootLocationId || p.LocationId == filter.RootLocationId);
        if (filter.EmployeeId.HasValue)    q = q.Where(p => p.EmployeeId == filter.EmployeeId);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            q = q.Where(p => p.StatusType!.Name == filter.Status);

        return await q.OrderBy(p => p.WorkDate).ThenBy(p => p.ScheduleId).ToListAsync(ct);
    }

    public async Task<GuardShiftPlanning?> GetWithChangesAsync(int planningId, CancellationToken ct) =>
        await _db.GuardShiftPlannings
            .Include(p => p.Employee).ThenInclude(e => e!.People)
            .Include(p => p.Location)
            .Include(p => p.Schedule)
            .Include(p => p.Changes)
                .ThenInclude(c => c.ReplacementEmployee).ThenInclude(e => e!.People)
            .FirstOrDefaultAsync(p => p.PlanningId == planningId, ct);

    public async Task<bool> HasActiveShiftOnDateAsync(int employeeId, DateOnly workDate, int? excludePlanningId, CancellationToken ct) =>
        await _db.GuardShiftPlannings
            .AnyAsync(p => p.EmployeeId == employeeId && p.WorkDate == workDate
                && p.IsActiveForAssignment
                && (excludePlanningId == null || p.PlanningId != excludePlanningId), ct);

    public async Task<List<GuardShiftPlanning>> GetByEmployeeAndDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate, CancellationToken ct) =>
        await _db.GuardShiftPlannings
            .Where(p => p.EmployeeId == employeeId && p.WorkDate >= startDate && p.WorkDate <= endDate)
            .Include(p => p.Schedule)
            .OrderBy(p => p.WorkDate)
            .ToListAsync(ct);

    public async Task<List<GuardShiftPlanning>> GetByGroupAndDateRangeAsync(int groupId, DateOnly startDate, DateOnly endDate, CancellationToken ct) =>
        await _db.GuardShiftPlannings
            .Where(p => p.GroupId == groupId && p.WorkDate >= startDate && p.WorkDate <= endDate)
            .Include(p => p.Employee).ThenInclude(e => e!.People)
            .Include(p => p.Schedule)
            .OrderBy(p => p.WorkDate).ThenBy(p => p.EmployeeId)
            .ToListAsync(ct);
}

public class GuardShiftChangeRepository
    : ServiceAwareEfRepository<GuardShiftChange, int>, IGuardShiftChangeRepository
{
    private readonly AppDbContext _db;
    public GuardShiftChangeRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<List<GuardShiftChange>> GetByPlanningIdAsync(int planningId, CancellationToken ct) =>
        await _db.GuardShiftChanges
            .Where(c => c.PlanningId == planningId)
            .Include(c => c.ReplacementEmployee).ThenInclude(e => e!.People)
            .Include(c => c.ChangeType)
            .Include(c => c.StatusType)
            .OrderByDescending(c => c.RequestedAt)
            .ToListAsync(ct);

    public async Task<GuardShiftChange?> GetActiveAttendanceChangeAsync(int planningId, CancellationToken ct) =>
        await _db.GuardShiftChanges
            .Where(c => c.PlanningId == planningId && c.IsActiveForAttendance)
            .Include(c => c.ReplacementEmployee).ThenInclude(e => e!.People)
            .FirstOrDefaultAsync(ct);

    public async Task<List<GuardShiftChange>> GetPendingChangesAsync(CancellationToken ct) =>
        await _db.GuardShiftChanges
            .Include(c => c.Planning).ThenInclude(p => p!.Location)
            .Include(c => c.OriginalEmployee).ThenInclude(e => e!.People)
            .Include(c => c.ReplacementEmployee).ThenInclude(e => e!.People)
            .Include(c => c.StatusType)
            .Where(c => c.StatusType!.Name == "PENDING")
            .OrderBy(c => c.RequestedAt)
            .ToListAsync(ct);
}

public class EmployeeAvailabilityBlockRepository
    : ServiceAwareEfRepository<EmployeeAvailabilityBlock, int>, IEmployeeAvailabilityBlockRepository
{
    private readonly AppDbContext _db;
    public EmployeeAvailabilityBlockRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<List<EmployeeAvailabilityBlock>> GetActiveBlocksAsync(int employeeId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct) =>
        await _db.EmployeeAvailabilityBlocks
            .Where(b => b.EmployeeId == employeeId
                && b.StatusType!.Name == "ACTIVE"
                && b.StartDateTime < endDateTime
                && b.EndDateTime > startDateTime)
            .Include(b => b.SourceType)
            .ToListAsync(ct);

    public async Task<bool> HasActiveBlockAsync(int employeeId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct) =>
        await _db.EmployeeAvailabilityBlocks
            .AnyAsync(b => b.EmployeeId == employeeId
                && b.StatusType!.Name == "ACTIVE"
                && b.StartDateTime < endDateTime
                && b.EndDateTime > startDateTime, ct);

    public async Task<List<EmployeeAvailabilityBlock>> GetBySourceAsync(string sourceTable, string sourceId, CancellationToken ct) =>
        await _db.EmployeeAvailabilityBlocks
            .Where(b => b.SourceTable == sourceTable && b.SourceId == sourceId)
            .ToListAsync(ct);

    public async Task CancelBySourceAsync(string sourceTable, string sourceId, int updatedBy, CancellationToken ct)
    {
        var cancelledTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_STATUS" && r.Name == "CANCELLED")
            .Select(r => r.TypeId)
            .FirstOrDefaultAsync(ct);

        var blocks = await _db.EmployeeAvailabilityBlocks
            .Where(b => b.SourceTable == sourceTable && b.SourceId == sourceId
                && b.StatusType!.Name == "ACTIVE")
            .ToListAsync(ct);

        foreach (var block in blocks)
        {
            block.StatusTypeId = cancelledTypeId;
            block.UpdatedBy = updatedBy;
            block.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }
}

public class GuardAssignmentValidationRepository
    : ServiceAwareEfRepository<GuardAssignmentValidation, long>, IGuardAssignmentValidationRepository
{
    private readonly AppDbContext _db;
    public GuardAssignmentValidationRepository(AppDbContext db) : base(db) => _db = db;

    public async Task<List<GuardAssignmentValidation>> GetByPlanningIdAsync(int planningId, CancellationToken ct) =>
        await _db.GuardAssignmentValidations
            .Where(v => v.PlanningId == planningId)
            .Include(v => v.ValidationType)
            .Include(v => v.ResultType)
            .Include(v => v.SeverityType)
            .OrderBy(v => v.ValidationDate)
            .ToListAsync(ct);

    public async Task<List<GuardAssignmentValidation>> GetByEmployeeIdAsync(int employeeId, int limit, CancellationToken ct) =>
        await _db.GuardAssignmentValidations
            .Where(v => v.EmployeeId == employeeId)
            .Include(v => v.ValidationType)
            .Include(v => v.ResultType)
            .Include(v => v.SeverityType)
            .OrderByDescending(v => v.ValidationDate)
            .Take(limit)
            .ToListAsync(ct);

    public async Task DeleteByPlanningIdAsync(int planningId, CancellationToken ct)
    {
        var validations = await _db.GuardAssignmentValidations
            .Where(v => v.PlanningId == planningId)
            .ToListAsync(ct);
        _db.GuardAssignmentValidations.RemoveRange(validations);
        await _db.SaveChangesAsync(ct);
    }
}

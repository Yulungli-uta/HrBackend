using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Interfaces.Guards;

public interface IGuardServiceLocationRepository : IRepository<GuardServiceLocation, int>
{
    Task<List<GuardServiceLocation>> GetTreeAsync(CancellationToken ct);
    Task<List<GuardServiceLocation>> GetAssignableAsync(CancellationToken ct);
    Task<List<GuardServiceLocation>> GetByRootAsync(int rootLocationId, CancellationToken ct);
}

public interface IGuardRotationGroupRepository : IRepository<GuardRotationGroup, int>
{
    Task<GuardRotationGroup?> GetWithEmployeesAsync(int groupId, CancellationToken ct);
    Task<List<GuardRotationGroupEmployee>> GetActiveEmployeesAsync(int groupId, DateOnly date, CancellationToken ct);

    /// <summary>Indica si el empleado pertenece, en la fecha dada, a algún grupo de rotación
    /// marcado como especial (IsSpecial=true) — usado para exceptuar la validación de doble turno.</summary>
    Task<bool> IsEmployeeInSpecialGroupAsync(int employeeId, DateOnly date, CancellationToken ct);
}

public interface IRotationPatternRepository : IRepository<RotationPattern, int>
{
    Task<RotationPattern?> GetWithDetailsAsync(int patternId, CancellationToken ct);
}

public interface IGuardShiftCoverageRequirementRepository : IRepository<GuardShiftCoverageRequirement, int>
{
    Task<List<GuardShiftCoverageRequirement>> GetByLocationAsync(int locationId, DateOnly date, CancellationToken ct);
}

public interface IGuardShiftPlanningRepository : IRepository<GuardShiftPlanning, int>
{
    Task<List<GuardShiftPlanning>> GetCalendarAsync(GuardShiftCalendarFilterDto filter, CancellationToken ct);
    Task<GuardShiftPlanning?> GetWithChangesAsync(int planningId, CancellationToken ct);
    Task<bool> HasActiveShiftOnDateAsync(int employeeId, DateOnly workDate, int? excludePlanningId, CancellationToken ct);
    Task<List<GuardShiftPlanning>> GetByEmployeeAndDateRangeAsync(int employeeId, DateOnly startDate, DateOnly endDate, CancellationToken ct);
    Task<List<GuardShiftPlanning>> GetByGroupAndDateRangeAsync(int groupId, DateOnly startDate, DateOnly endDate, CancellationToken ct);
}

public interface IGuardShiftChangeRepository : IRepository<GuardShiftChange, int>
{
    Task<List<GuardShiftChange>> GetByPlanningIdAsync(int planningId, CancellationToken ct);
    Task<GuardShiftChange?> GetActiveAttendanceChangeAsync(int planningId, CancellationToken ct);
    Task<List<GuardShiftChange>> GetPendingChangesAsync(CancellationToken ct);
}

public interface IEmployeeAvailabilityBlockRepository : IRepository<EmployeeAvailabilityBlock, int>
{
    Task<List<EmployeeAvailabilityBlock>> GetActiveBlocksAsync(int employeeId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct);
    Task<bool> HasActiveBlockAsync(int employeeId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct);
    Task<List<EmployeeAvailabilityBlock>> GetBySourceAsync(string sourceTable, string sourceId, CancellationToken ct);
    Task CancelBySourceAsync(string sourceTable, string sourceId, int updatedBy, CancellationToken ct);
}

public interface IGuardAssignmentValidationRepository : IRepository<GuardAssignmentValidation, long>
{
    Task<List<GuardAssignmentValidation>> GetByPlanningIdAsync(int planningId, CancellationToken ct);
    Task<List<GuardAssignmentValidation>> GetByEmployeeIdAsync(int employeeId, int limit, CancellationToken ct);
    Task DeleteByPlanningIdAsync(int planningId, CancellationToken ct);
}

public interface IGuardLocationRotationPeriodRepository : IRepository<GuardLocationRotationPeriod, int>
{
    Task<List<GuardLocationRotationPeriod>> GetActiveByDateAsync(DateOnly date, CancellationToken ct);
    Task<GuardLocationRotationPeriod?> GetWithAssignmentsAsync(int periodId, CancellationToken ct);
}

public interface IGuardLocationRotationAssignmentRepository : IRepository<GuardLocationRotationAssignment, int>
{
    Task<List<GuardLocationRotationAssignment>> GetByPeriodAsync(int periodId, CancellationToken ct);
    Task<GuardLocationRotationAssignment?> GetActiveForGroupAsync(int groupId, DateOnly date, CancellationToken ct);
    Task<GuardLocationRotationAssignment?> GetActiveForEmployeeAsync(int employeeId, DateOnly date, CancellationToken ct);
}

public interface IGuardEmployeeSpecialRuleRepository : IRepository<GuardEmployeeSpecialRule, int>
{
    Task<List<GuardEmployeeSpecialRule>> GetActiveByEmployeeAsync(int employeeId, DateOnly date, CancellationToken ct);
    Task<GuardEmployeeSpecialRule?> GetActiveRuleAsync(int employeeId, DateOnly date, CancellationToken ct);
}

public interface IGuardVacationPlanRepository : IRepository<GuardVacationPlan, int>
{
    Task<List<GuardVacationPlan>> GetByEmployeeAsync(int employeeId, int? year, CancellationToken ct);
    Task<List<GuardVacationPlan>> GetPendingApprovalAsync(CancellationToken ct);
}

public interface IGuardVacationRequestRepository : IRepository<GuardVacationRequest, int>
{
    Task<List<GuardVacationRequest>> GetByEmployeeAsync(int employeeId, CancellationToken ct);
    Task<List<GuardVacationRequest>> GetPendingDirectionApprovalAsync(CancellationToken ct);
}

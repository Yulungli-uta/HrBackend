using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Interfaces.Guards;

public interface IGuardServiceLocationService
{
    Task<List<GuardServiceLocationTreeDto>> GetTreeAsync(CancellationToken ct);
    Task<List<GuardServiceLocationDto>> GetAssignableAsync(CancellationToken ct);
    Task<GuardServiceLocationDto?> GetByIdAsync(int locationId, CancellationToken ct);
    Task<GuardServiceLocationDto> CreateAsync(CreateGuardServiceLocationDto dto, CancellationToken ct);
    Task<GuardServiceLocationDto> UpdateAsync(int locationId, UpdateGuardServiceLocationDto dto, CancellationToken ct);
}

public interface IGuardRotationGroupService
{
    Task<List<GuardRotationGroupDto>> GetAllAsync(CancellationToken ct);
    Task<PagedResult<GuardRotationGroupDto>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<GuardRotationGroupDto?> GetByIdAsync(int groupId, CancellationToken ct);
    Task<GuardRotationGroupDto> CreateAsync(CreateGuardRotationGroupDto dto, CancellationToken ct);
    Task<GuardRotationGroupDto> UpdateAsync(int groupId, UpdateGuardRotationGroupDto dto, CancellationToken ct);
    Task<List<GuardRotationGroupEmployeeDto>> GetEmployeesAsync(int groupId, CancellationToken ct);
    Task<GuardRotationGroupEmployeeDto> AssignEmployeeAsync(int groupId, AssignEmployeeToRotationGroupDto dto, CancellationToken ct);
    Task RemoveEmployeeAsync(int groupId, RemoveEmployeeFromRotationGroupDto dto, CancellationToken ct);
    Task<List<LocationSummaryDto>> GetLocationSummaryAsync(CancellationToken ct);
    Task<List<LocationGroupDetailDto>> GetByLocationKeyAsync(string locationKey, CancellationToken ct);
}

public interface IRotationPatternService
{
    Task<List<RotationPatternDto>> GetAllAsync(CancellationToken ct);
    Task<PagedResult<RotationPatternDto>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<RotationPatternDto?> GetByIdAsync(int patternId, CancellationToken ct);
    Task<RotationPatternDto> CreateAsync(CreateRotationPatternDto dto, CancellationToken ct);
    Task<RotationPatternDto> UpdateAsync(int patternId, UpdateRotationPatternDto dto, CancellationToken ct);
    Task<RotationPatternDto> SetDetailsAsync(int patternId, UpsertRotationPatternDetailsDto dto, CancellationToken ct);
}

public interface IGuardShiftCoverageRequirementService
{
    Task<List<GuardShiftCoverageRequirementDto>> GetAllAsync(CancellationToken ct);
    Task<PagedResult<GuardShiftCoverageRequirementDto>> GetPagedAsync(int page, int pageSize, CancellationToken ct);
    Task<GuardShiftCoverageRequirementDto?> GetByIdAsync(int requirementId, CancellationToken ct);
    Task<GuardShiftCoverageRequirementDto> CreateAsync(CreateCoverageRequirementDto dto, CancellationToken ct);
    Task<GuardShiftCoverageRequirementDto> UpdateAsync(int requirementId, UpdateCoverageRequirementDto dto, CancellationToken ct);
}

public interface IGuardShiftPlanningService
{
    Task<List<GuardShiftCalendarItemDto>> GetCalendarAsync(GuardShiftCalendarFilterDto filter, CancellationToken ct);
    Task<GuardShiftPlanningDto?> GetByIdAsync(int planningId, CancellationToken ct);
    Task<GuardShiftPlanningDetailDto?> GetPlanningDetailAsync(int planningId, CancellationToken ct);
    Task<GuardShiftPlanningDto> CreateAsync(CreateGuardShiftPlanningDto dto, CancellationToken ct);
    Task<GuardShiftPlanningResultDto> GenerateAsync(GenerateGuardShiftPlanningRequestDto dto, CancellationToken ct);
    Task<GeneratePreviewResponseDto> GeneratePreviewAsync(GeneratePreviewRequestDto dto, CancellationToken ct);
    Task<GuardShiftPlanningResultDto> GenerateConfirmAsync(GeneratePreviewRequestDto dto, CancellationToken ct);
    Task<ScheduleBoardResponseDto> GetScheduleBoardAsync(ScheduleBoardFilterDto filter, CancellationToken ct);
    Task<ValidateGuardAssignmentResultDto> ValidateAssignmentAsync(ValidateGuardAssignmentRequestDto dto, CancellationToken ct);
    Task<GuardDashboardDto> GetDashboardAsync(CancellationToken ct);
}

public interface IGuardShiftChangeService
{
    Task<List<GuardShiftChangeDto>> GetByPlanningAsync(int planningId, CancellationToken ct);
    Task<List<GuardShiftChangeDto>> GetPendingAsync(CancellationToken ct);
    Task<PagedResult<GuardShiftChangeDto>> GetPendingPagedAsync(int page, int pageSize, CancellationToken ct);
    Task<GuardShiftChangeDto> CreateReplacementAsync(CreateGuardShiftReplacementDto dto, CancellationToken ct);
    Task<GuardShiftChangeDto> ApproveAsync(int shiftChangeId, ApproveGuardShiftChangeDto dto, CancellationToken ct);
    Task<GuardShiftChangeDto> RejectAsync(int shiftChangeId, RejectGuardShiftChangeDto dto, CancellationToken ct);
}

public interface IEmployeeAvailabilityService
{
    Task<List<EmployeeAvailabilityBlockDto>> GetBlocksAsync(EmployeeAvailabilityFilterDto filter, CancellationToken ct);
    Task<PagedResult<EmployeeAvailabilityBlockDto>> GetBlocksPagedAsync(EmployeeAvailabilityFilterDto filter, int page, int pageSize, CancellationToken ct);
    Task<EmployeeAvailabilityBlockDto> CreateManualBlockAsync(CreateManualAvailabilityBlockDto dto, CancellationToken ct);
    Task<SyncAvailabilityBlocksResultDto> SyncPermissionsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct);
    Task<SyncAvailabilityBlocksResultDto> SyncVacationsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct);
    Task<bool> HasBlockAsync(int employeeId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct);
}

public interface IGuardAssignmentValidationService
{
    Task<List<GuardAssignmentValidationDto>> GetByPlanningAsync(int planningId, CancellationToken ct);
    Task<PagedResult<GuardAssignmentValidationDto>> GetByPlanningPagedAsync(int planningId, int page, int pageSize, CancellationToken ct);
    Task<List<GuardAssignmentValidationDto>> GetByEmployeeAsync(int employeeId, int limit, CancellationToken ct);
    Task<ValidateGuardAssignmentResultDto> ValidateAsync(ValidateGuardAssignmentRequestDto dto, CancellationToken ct);
}

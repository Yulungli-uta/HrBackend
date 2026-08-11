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
    Task<List<GuardGroupRotationPatternDto>> GetGroupPatternsAsync(int groupId, CancellationToken ct);
    Task<GuardGroupRotationPatternDto> AssignPatternToGroupAsync(int groupId, AssignPatternToGroupDto dto, CancellationToken ct);
    Task RemovePatternFromGroupAsync(int groupId, int groupPatternId, CancellationToken ct);
    Task<List<GuardRotationGroupDto>> GetGeneralGroupsAsync(CancellationToken ct);
    Task<List<GuardRotationGroupWithSubgroupsDto>> GetGeneralGroupsWithSubgroupsAsync(CancellationToken ct);
    Task<List<GuardRotationGroupDto>> GetSubgroupsByParentAsync(int parentGroupId, CancellationToken ct);

    /// <summary>Empleados con cargo de guardia (ver GuardRotationGroupService.GuardJobNames), para el
    /// buscador de "Agregar guardias" — no usa el buscador genérico de empleados.</summary>
    Task<List<EligibleEmployeeDto>> GetEligibleEmployeesAsync(string? search, CancellationToken ct);

    /// <summary>Crea un grupo nuevo copiando configuración (y empleados activos) de un grupo base existente.</summary>
    Task<GuardRotationGroupDto> DuplicateAsync(int baseGroupId, DuplicateGuardRotationGroupDto dto, CancellationToken ct);
}

public interface IRotationPatternService
{
    Task<List<RotationPatternDto>> GetAllAsync(CancellationToken ct);
    Task<PagedResult<RotationPatternDto>> GetPagedAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct);
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
    /// <summary>
    /// Verifica los pre-requisitos de configuración necesarios para generar planificación
    /// en el rango de fechas indicado.
    /// </summary>
    Task<GuardReadinessCheckDto> GetReadinessCheckAsync(DateOnly targetDate, CancellationToken ct);
    Task<GuardShiftPlanningDetailDto?> GetPlanningDetailAsync(int planningId, CancellationToken ct);
    Task<GuardShiftPlanningDto> CreateAsync(CreateGuardShiftPlanningDto dto, CancellationToken ct);
    Task<GuardShiftPlanningResultDto> GenerateAsync(GenerateGuardShiftPlanningRequestDto dto, CancellationToken ct);
    Task<GeneratePreviewResponseDto> GeneratePreviewAsync(GeneratePreviewRequestDto dto, CancellationToken ct);
    Task<GuardShiftPlanningResultDto> GenerateConfirmAsync(GeneratePreviewRequestDto dto, CancellationToken ct);
    Task<ScheduleBoardResponseDto> GetScheduleBoardAsync(ScheduleBoardFilterDto filter, CancellationToken ct);
    Task<ValidateGuardAssignmentResultDto> ValidateAssignmentAsync(ValidateGuardAssignmentRequestDto dto, CancellationToken ct);
    Task<GuardDashboardDto> GetDashboardAsync(CancellationToken ct);

    /// <summary>
    /// Cancela una planificación individual (no la borra): marca IsActiveForAssignment=false
    /// y StatusTypeId=CANCELLED, liberando la fecha para volver a planificarse.
    /// </summary>
    Task<GuardShiftPlanningDto> CancelPlanningAsync(int planningId, CancelGuardShiftPlanningDto dto, CancellationToken ct);

    /// <summary>
    /// Cancela en bloque todas las planificaciones activas de un grupo en un rango de fechas.
    /// </summary>
    Task<CancelGuardShiftPlanningResultDto> CancelPlanningRangeAsync(CancelGuardShiftPlanningRangeDto dto, CancellationToken ct);
}

public interface IGuardShiftChangeService
{
    Task<List<GuardShiftChangeDto>> GetByPlanningAsync(int planningId, CancellationToken ct);
    Task<List<GuardShiftChangeDto>> GetPendingAsync(CancellationToken ct);
    Task<PagedResult<GuardShiftChangeDto>> GetPendingPagedAsync(int page, int pageSize, CancellationToken ct);
    Task<PagedResult<GuardShiftChangeDto>> GetAllPagedAsync(int page, int pageSize, string? status, CancellationToken ct);
    Task<GuardShiftChangeDto> CreateReplacementAsync(CreateGuardShiftReplacementDto dto, CancellationToken ct);
    Task<GuardShiftChangeDto> ApproveAsync(int shiftChangeId, ApproveGuardShiftChangeDto dto, CancellationToken ct);
    Task<GuardShiftChangeDto> RejectAsync(int shiftChangeId, RejectGuardShiftChangeDto dto, CancellationToken ct);

    /// <summary>Reasigna el turno del mismo guardia titular a otra fecha/horario/ubicación. Aplicación
    /// inmediata (sin aprobación); queda registrado como GuardShiftChange tipo REASSIGNMENT.</summary>
    Task<GuardShiftChangeDto> ReassignAsync(CreateGuardShiftReassignmentDto dto, CancellationToken ct);
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

public interface IGuardLocationRotationService
{
    Task<List<GuardLocationRotationPeriodDto>> GetPeriodsAsync(CancellationToken ct);
    Task<PagedResult<GuardLocationRotationPeriodDto>> GetPeriodsPagedAsync(int page, int pageSize, CancellationToken ct);
    Task<GuardLocationRotationPeriodDto?> GetPeriodByIdAsync(int periodId, CancellationToken ct);
    Task<GuardLocationRotationPeriodDto> CreatePeriodAsync(CreateGuardLocationRotationPeriodDto dto, CancellationToken ct);
    Task<GuardLocationRotationPeriodDto> UpdatePeriodAsync(int periodId, UpdateGuardLocationRotationPeriodDto dto, CancellationToken ct);
    Task<List<GuardLocationRotationAssignmentDto>> GetAssignmentsByPeriodAsync(int periodId, CancellationToken ct);
    Task<List<GuardLocationRotationAssignmentDto>> GetAssignmentsByEmployeeAsync(int employeeId, CancellationToken ct);
    Task<GuardLocationRotationAssignmentDto> CreateAssignmentAsync(CreateGuardLocationRotationAssignmentDto dto, CancellationToken ct);
    Task<GuardLocationRotationAssignmentDto> UpdateAssignmentAsync(int assignmentId, UpdateGuardLocationRotationAssignmentDto dto, CancellationToken ct);
    Task DeleteAssignmentAsync(int assignmentId, CancellationToken ct);
}

public interface IGuardEmployeeSpecialRuleService
{
    Task<List<GuardEmployeeSpecialRuleDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct);
    Task<PagedResult<GuardEmployeeSpecialRuleDto>> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<GuardEmployeeSpecialRuleDto?> GetByIdAsync(int ruleId, CancellationToken ct);
    Task<GuardEmployeeSpecialRuleDto> CreateAsync(CreateGuardEmployeeSpecialRuleDto dto, CancellationToken ct);
    Task<GuardEmployeeSpecialRuleDto> UpdateAsync(int ruleId, UpdateGuardEmployeeSpecialRuleDto dto, CancellationToken ct);
}

public interface IGuardVacationService
{
    Task<List<GuardVacationPlanDto>> GetPlansByEmployeeAsync(int employeeId, int? year, CancellationToken ct);
    Task<PagedResult<GuardVacationPlanDto>> GetPlansPagedAsync(int page, int pageSize, int? year, string? status, int? employeeId, DateOnly? startDate, DateOnly? endDate, CancellationToken ct);
    Task<GuardVacationPlanDto?> GetPlanByIdAsync(int planId, CancellationToken ct);
    Task<GuardVacationPlanDto> CreatePlanAsync(CreateGuardVacationPlanDto dto, CancellationToken ct);
    Task<GuardVacationPlanDto> UpdatePlanAsync(int planId, UpdateGuardVacationPlanDto dto, CancellationToken ct);
    Task<GuardVacationPlanDto> ApprovePlanAsync(int planId, ApproveGuardVacationPlanDto dto, CancellationToken ct);
    Task<GuardVacationPlanDto> RejectPlanAsync(int planId, RejectGuardVacationPlanDto dto, CancellationToken ct);
    Task<List<GuardVacationRequestDto>> GetRequestsByEmployeeAsync(int employeeId, CancellationToken ct);
    Task<PagedResult<GuardVacationRequestDto>> GetRequestsPagedAsync(int page, int pageSize, string? status, int? employeeId, DateOnly? startDate, DateOnly? endDate, CancellationToken ct);
    Task<GuardVacationRequestDto?> GetRequestByIdAsync(int requestId, CancellationToken ct);
    Task<GuardVacationRequestDto> CreateChangeDatesRequestAsync(CreateChangeDatesRequestDto dto, CancellationToken ct);
    Task<GuardVacationRequestDto> CreateAccumulateRequestAsync(CreateAccumulateRequestDto dto, CancellationToken ct);
    Task<GuardVacationPlanDto> SubmitPlanToDirectionAsync(int planId, SubmitToDirectionDto dto, CancellationToken ct);
    Task<GuardVacationRequestDto> SubmitRequestToDirectionAsync(int requestId, SubmitToDirectionDto dto, CancellationToken ct);
    Task<GuardVacationRequestDto> ApproveRequestAsync(int requestId, ApproveGuardVacationRequestDto dto, CancellationToken ct);
    Task<GuardVacationRequestDto> RejectRequestAsync(int requestId, RejectGuardVacationRequestDto dto, CancellationToken ct);
}

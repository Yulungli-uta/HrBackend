using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.TimePlanning;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <inheritdoc />
public sealed class TimePlanningOverviewService : ITimePlanningOverviewService
{
    private const string ScopeModuleCode = "TIME_PLANNING";

    private readonly ITimePlanningOverviewRepository _repository;
    private readonly IUserAccessScopeService _accessScope;

    public TimePlanningOverviewService(ITimePlanningOverviewRepository repository, IUserAccessScopeService accessScope)
    {
        _repository = repository;
        _accessScope = accessScope;
    }

    /// <inheritdoc />
    public async Task<PagedResult<TimePlanningOverviewItemDto>> GetPlansAsync(
        TimePlanningOverviewFilter filter, int? requesterEmployeeId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (filter.StartDate.HasValue && filter.EndDate.HasValue && filter.StartDate > filter.EndDate)
            throw new ArgumentException("La fecha de inicio no puede ser posterior a la fecha de fin.");

        var allowed = await ResolveAllowedDepartmentsAsync(requesterEmployeeId, ct);
        return await _repository.GetPlansPagedAsync(filter with { AllowedDepartmentIds = allowed }, ct);
    }

    /// <inheritdoc />
    public async Task<PagedResult<TimePlanningOverviewEmployeeDto>> GetPlanEmployeesAsync(
        int planId, int page, int pageSize, int? employeeStatusTypeId, string? search, int? requesterEmployeeId, CancellationToken ct = default)
    {
        if (planId <= 0) throw new ArgumentException("El identificador de la planificación no es válido.");

        var allowed = await ResolveAllowedDepartmentsAsync(requesterEmployeeId, ct);
        return await _repository.GetPlanEmployeesPagedAsync(planId, page, pageSize, employeeStatusTypeId, search, allowed, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimePlanningOverviewExecutionDto>> GetPlanEmployeeExecutionsAsync(
        int planEmployeeId, int? requesterEmployeeId, CancellationToken ct = default)
    {
        if (planEmployeeId <= 0) throw new ArgumentException("El identificador del empleado del plan no es válido.");

        var allowed = await ResolveAllowedDepartmentsAsync(requesterEmployeeId, ct);
        return await _repository.GetPlanEmployeeExecutionsAsync(planEmployeeId, allowed, ct);
    }

    // null = sin restricción (alcance GLOBAL o sin scopes asignados), igual que Acciones de Personal.
    private async Task<IReadOnlyList<int>?> ResolveAllowedDepartmentsAsync(int? requesterEmployeeId, CancellationToken ct)
        => requesterEmployeeId.HasValue
            ? await _accessScope.GetAllowedDepartmentIdsAsync(requesterEmployeeId.Value, ScopeModuleCode, ct)
            : null;
}

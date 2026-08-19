using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.MassVacationPlan;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IMassVacationPlanRepository : IRepository<MassVacationPlan, int>
{
    /// <summary>Empleados activos del alcance del plan (institucional o por departamento), con su estado de exclusión actual.</summary>
    Task<List<MassVacationPlanRosterItemDto>> GetRosterAsync(int planId, CancellationToken ct);

    Task<MassVacationPlanExclusion?> GetExclusionAsync(int planId, int employeeId, CancellationToken ct);
    Task AddExclusionAsync(MassVacationPlanExclusion exclusion, CancellationToken ct);
    Task RemoveExclusionAsync(MassVacationPlanExclusion exclusion, CancellationToken ct);

    /// <summary>EmployeeId de los empleados activos del alcance que NO están excluidos — los que sí se procesan al ejecutar.</summary>
    Task<List<int>> GetIncludedEmployeeIdsAsync(int planId, CancellationToken ct);
}

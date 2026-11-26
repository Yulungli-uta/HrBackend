using WsUtaSystem.Application.DTOs.PunchPolicy;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Acceso a datos del control de picadas por red (redes del campus, permisos externos, registro).</summary>
public interface IPunchPolicyRepository
{
    // Redes del campus (catálogo pequeño: se lista completo)
    Task<List<AllowedPunchNetwork>> GetNetworksAsync(CancellationToken ct);
    Task<AllowedPunchNetwork?> GetNetworkAsync(int networkId, CancellationToken ct);
    Task<bool> NetworkExistsAsync(string ipOrCidr, int? excludeNetworkId, CancellationToken ct);
    Task<AllowedPunchNetwork> AddNetworkAsync(AllowedPunchNetwork network, CancellationToken ct);
    Task UpdateNetworkAsync(AllowedPunchNetwork network, CancellationToken ct);
    Task<bool> DeleteNetworkAsync(int networkId, CancellationToken ct);

    // Autorizaciones de picada externa (historial: varias filas por empleado, nunca se borran)
    /// <summary>Autorización vigente hoy del empleado (no revocada y dentro de su vigencia), si existe.</summary>
    Task<EmployeePunchPolicy?> GetEffectivePolicyAsync(int employeeId, DateTime today, CancellationToken ct);
    Task<EmployeePunchPolicy?> GetPolicyAsync(int policyId, CancellationToken ct);
    Task<(List<PunchPolicyItemDto> Items, long Total)> GetPoliciesPagedAsync(
        string? search, bool onlyEffective, int? employeeId, Guid? batchId, DateTime today, int page, int pageSize, CancellationToken ct);
    Task<PunchPolicyItemDto?> GetPolicyItemAsync(int policyId, DateTime today, CancellationToken ct);
    /// <summary>Empleados (de los indicados) que ya tienen una autorización no revocada que se cruza con la vigencia.</summary>
    Task<HashSet<int>> GetEmployeesWithOverlapAsync(IReadOnlyCollection<int> employeeIds, DateTime? from, DateTime? to, int? excludePolicyId, CancellationToken ct);
    /// <summary>true si ya existe una autorización global no revocada que se cruza con la vigencia.</summary>
    Task<bool> HasGlobalOverlapAsync(DateTime? from, DateTime? to, CancellationToken ct);
    Task AddPoliciesAsync(IReadOnlyCollection<EmployeePunchPolicy> policies, CancellationToken ct);
    Task SavePoliciesAsync(CancellationToken ct);
    /// <summary>Autorizaciones no revocadas de un lote.</summary>
    Task<List<EmployeePunchPolicy>> GetOpenPoliciesOfBatchAsync(Guid batchId, CancellationToken ct);
    Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken ct);
    /// <summary>Nombre del departamento activo, o null si no existe.</summary>
    Task<string?> GetDepartmentNameAsync(int departmentId, CancellationToken ct);
    /// <summary>Empleados activos asignados al departamento (y a sus hijos si se pide) y cuántos departamentos abarca.</summary>
    Task<(List<int> EmployeeIds, int Departments)> GetEmployeesOfDepartmentAsync(int departmentId, bool includeChildren, CancellationToken ct);

    // Registro de picadas fuera del campus
    Task AddLogAsync(PunchAccessLog log, CancellationToken ct);
    Task<(List<PunchAccessLogItemDto> Items, long Total)> GetLogPagedAsync(
        DateTime? from, DateTime? to, int? employeeId, string? result, int page, int pageSize, CancellationToken ct);

    // Modo de operación (HR.tbl_Parameters: PUNCH_NETWORK_MODE)
    Task<string?> GetModeAsync(CancellationToken ct);
    /// <summary>Valor de un parámetro activo de HR.tbl_Parameters (null si no existe).</summary>
    Task<string?> GetParameterAsync(string name, CancellationToken ct);
    Task SetModeAsync(string mode, CancellationToken ct);
}

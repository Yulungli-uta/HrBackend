using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

public class RecordAccessGuard : IRecordAccessGuard
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserAccessScopeService _scope;
    private readonly IvwEmployeeDetailsService _employeeDetails;

    public RecordAccessGuard(
        ICurrentUserService currentUser,
        IUserAccessScopeService scope,
        IvwEmployeeDetailsService employeeDetails)
    {
        _currentUser = currentUser;
        _scope = scope;
        _employeeDetails = employeeDetails;
    }

    public async Task EnsureDepartmentAsync(int departmentId, string moduleCode, CancellationToken ct = default)
    {
        var callerEmployeeId = _currentUser.EmployeeId
            ?? throw new UnauthorizedAccessException("Usuario sin EmployeeId no puede acceder a este recurso.");

        await _scope.EnsureDepartmentAllowedAsync(callerEmployeeId, moduleCode, departmentId, ct);
    }

    public async Task EnsureEmployeeRecordAsync(int recordOwnerEmployeeId, string moduleCode, CancellationToken ct = default)
    {
        var callerEmployeeId = _currentUser.EmployeeId
            ?? throw new UnauthorizedAccessException("Usuario sin EmployeeId no puede acceder a este recurso.");

        // Acceder a tu propio registro siempre está permitido, sin necesidad de scope.
        if (callerEmployeeId == recordOwnerEmployeeId) return;

        var owner = await _employeeDetails.GetEmployeeDetailsAsync(recordOwnerEmployeeId, ct);
        if (owner?.DepartmentID is null)
            throw new UnauthorizedAccessException("No se pudo determinar el departamento del registro solicitado.");

        await _scope.EnsureDepartmentAllowedAsync(callerEmployeeId, moduleCode, owner.DepartmentID.Value, ct);
    }
}

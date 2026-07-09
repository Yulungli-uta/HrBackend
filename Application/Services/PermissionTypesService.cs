using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class PermissionTypesService : Service<PermissionTypes, int>, IPermissionTypesService
{
    private readonly IPermissionTypesRepository _typedRepo;

    public PermissionTypesService(IPermissionTypesRepository repo) : base(repo)
        => _typedRepo = repo;

    /// <inheritdoc/>
    public Task<IEnumerable<PermissionTypes>> GetAvailableForEmployeeAsync(
        int employeeId, CancellationToken ct)
        => _typedRepo.GetAvailableForEmployeeAsync(employeeId, ct);
}

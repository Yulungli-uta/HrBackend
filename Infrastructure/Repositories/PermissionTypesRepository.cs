using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class PermissionTypesRepository : ServiceAwareEfRepository<PermissionTypes, int>, IPermissionTypesRepository
{
    private readonly AppDbContext _db;

    public PermissionTypesRepository(AppDbContext db) : base(db) => _db = db;

    /// <inheritdoc/>
    public async Task<IEnumerable<PermissionTypes>> GetAvailableForEmployeeAsync(
        int employeeId,
        CancellationToken ct)
    {
        // Regímenes a incluir: todos los activos del empleado (HR.tbl_EmployeeLaborRegime) + los NULL (aplican a todos).
        var allowedIds = await _db.EmployeeLaborRegimes
            .AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && r.IsActive)
            .Select(r => r.LaborRegimeId)
            .Distinct()
            .ToListAsync(ct);

        return await _db.PermissionTypes
            .AsNoTracking()
            .Where(p => p.IsActive &&
                        (p.ContractTypeId == null || allowedIds.Contains(p.ContractTypeId.Value)))
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }
}

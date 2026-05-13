using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class EmployeeAvailabilityService : IEmployeeAvailabilityService
{
    private readonly IEmployeeAvailabilityBlockRepository _repo;
    private readonly AppDbContext _db;

    public EmployeeAvailabilityService(IEmployeeAvailabilityBlockRepository repo, AppDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task<List<EmployeeAvailabilityBlockDto>> GetBlocksAsync(EmployeeAvailabilityFilterDto filter, CancellationToken ct)
    {
        var q = _db.EmployeeAvailabilityBlocks
            .Include(b => b.Employee).ThenInclude(e => e!.People)
            .Include(b => b.SourceType)
            .Include(b => b.StatusType)
            .AsQueryable();

        if (filter.EmployeeId.HasValue) q = q.Where(b => b.EmployeeId == filter.EmployeeId.Value);
        if (filter.StartDate.HasValue)  q = q.Where(b => b.EndDateTime >= filter.StartDate.Value.ToDateTime(TimeOnly.MinValue));
        if (filter.EndDate.HasValue)    q = q.Where(b => b.StartDateTime <= filter.EndDate.Value.ToDateTime(TimeOnly.MaxValue));
        if (!string.IsNullOrWhiteSpace(filter.SourceType)) q = q.Where(b => b.SourceType!.Name == filter.SourceType);
        if (!string.IsNullOrWhiteSpace(filter.Status))     q = q.Where(b => b.StatusType!.Name == filter.Status);

        var blocks = await q.OrderByDescending(b => b.StartDateTime).ToListAsync(ct);

        return blocks.Select(b => new EmployeeAvailabilityBlockDto(
            b.BlockId, b.EmployeeId,
            $"{b.Employee?.People?.FirstName} {b.Employee?.People?.LastName}",
            b.SourceType?.Name ?? "", b.SourceTable, b.SourceId,
            b.StartDateTime, b.EndDateTime, b.StatusType?.Name ?? "", b.Reason
        )).ToList();
    }

    public async Task<PagedResult<EmployeeAvailabilityBlockDto>> GetBlocksPagedAsync(
        EmployeeAvailabilityFilterDto filter, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.EmployeeAvailabilityBlocks
            .Include(b => b.Employee).ThenInclude(e => e!.People)
            .Include(b => b.SourceType)
            .Include(b => b.StatusType)
            .AsQueryable();

        if (filter.EmployeeId.HasValue) q = q.Where(b => b.EmployeeId == filter.EmployeeId.Value);
        if (filter.StartDate.HasValue)  q = q.Where(b => b.EndDateTime >= filter.StartDate.Value.ToDateTime(TimeOnly.MinValue));
        if (filter.EndDate.HasValue)    q = q.Where(b => b.StartDateTime <= filter.EndDate.Value.ToDateTime(TimeOnly.MaxValue));
        if (!string.IsNullOrWhiteSpace(filter.SourceType)) q = q.Where(b => b.SourceType!.Name == filter.SourceType);
        if (!string.IsNullOrWhiteSpace(filter.Status))     q = q.Where(b => b.StatusType!.Name == filter.Status);

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderByDescending(b => b.StartDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<EmployeeAvailabilityBlockDto>
        {
            Items = items.Select(b => new EmployeeAvailabilityBlockDto(
                b.BlockId, b.EmployeeId,
                $"{b.Employee?.People?.FirstName} {b.Employee?.People?.LastName}",
                b.SourceType?.Name ?? "", b.SourceTable, b.SourceId,
                b.StartDateTime, b.EndDateTime, b.StatusType?.Name ?? "", b.Reason
            )).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<EmployeeAvailabilityBlockDto> CreateManualBlockAsync(CreateManualAvailabilityBlockDto dto, CancellationToken ct)
    {
        var emp = await _db.Set<WsUtaSystem.Models.Employees>()
            .Include(e => e.People)
            .FirstOrDefaultAsync(e => e.EmployeeId == dto.EmployeeId, ct)
            ?? throw new KeyNotFoundException($"Empleado {dto.EmployeeId} no encontrado.");

        var activeTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_STATUS" && r.Name == "ACTIVE")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var entity = new EmployeeAvailabilityBlock
        {
            EmployeeId = dto.EmployeeId,
            SourceTypeId = dto.SourceTypeId,
            StartDateTime = dto.StartDateTime,
            EndDateTime = dto.EndDateTime,
            StatusTypeId = activeTypeId,
            Reason = dto.Reason
        };

        await _db.EmployeeAvailabilityBlocks.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return new EmployeeAvailabilityBlockDto(
            entity.BlockId, entity.EmployeeId,
            $"{emp.People?.FirstName} {emp.People?.LastName}",
            "", null, null, entity.StartDateTime, entity.EndDateTime, "ACTIVE", entity.Reason
        );
    }

    public async Task<SyncAvailabilityBlocksResultDto> SyncPermissionsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var permSourceTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_SOURCE" && r.Name == "PERMISSION")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var activeTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_STATUS" && r.Name == "ACTIVE")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var start = startDate.ToDateTime(TimeOnly.MinValue);
        var end = endDate.ToDateTime(TimeOnly.MaxValue);

        var approvedPermissions = await _db.Set<WsUtaSystem.Models.Permissions>()
            .Where(p => p.Status == "Approved"
                && p.EndDate >= start && p.StartDate <= end)
            .ToListAsync(ct);

        int created = 0, updated = 0;

        foreach (var perm in approvedPermissions)
        {
            var sourceId = perm.PermissionId.ToString();
            var existing = await _repo.GetBySourceAsync("HR.tbl_Permissions", sourceId, ct);

            if (!existing.Any())
            {
                var block = new EmployeeAvailabilityBlock
                {
                    EmployeeId = perm.EmployeeId,
                    SourceTypeId = permSourceTypeId,
                    SourceTable = "HR.tbl_Permissions",
                    SourceId = sourceId,
                    StartDateTime = perm.StartDate,
                    EndDateTime = perm.EndDate,
                    StatusTypeId = activeTypeId,
                    Reason = "Permiso aprobado"
                };
                await _db.EmployeeAvailabilityBlocks.AddAsync(block, ct);
                created++;
            }
            else
            {
                foreach (var b in existing.Where(b => b.StatusTypeId != activeTypeId))
                {
                    b.StatusTypeId = activeTypeId;
                    b.StartDateTime = perm.StartDate;
                    b.EndDateTime = perm.EndDate;
                    updated++;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return new SyncAvailabilityBlocksResultDto(created, updated, 0, new List<string> { $"Sincronización de permisos: {created} creados, {updated} actualizados." });
    }

    public async Task<SyncAvailabilityBlocksResultDto> SyncVacationsAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var vacSourceTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_SOURCE" && r.Name == "VACATION")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var activeTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_STATUS" && r.Name == "ACTIVE")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var approvedVacations = await _db.Set<WsUtaSystem.Models.Vacations>()
            .Where(v => v.Status == "InProgress" || v.Status == "Planned")
            .Where(v => v.EndDate >= startDate && v.StartDate <= endDate)
            .ToListAsync(ct);

        int created = 0, updated = 0;

        foreach (var vac in approvedVacations)
        {
            var sourceId = vac.VacationId.ToString();
            var existing = await _repo.GetBySourceAsync("HR.tbl_Vacations", sourceId, ct);

            if (!existing.Any())
            {
                var block = new EmployeeAvailabilityBlock
                {
                    EmployeeId = vac.EmployeeId,
                    SourceTypeId = vacSourceTypeId,
                    SourceTable = "HR.tbl_Vacations",
                    SourceId = sourceId,
                    StartDateTime = vac.StartDate.ToDateTime(TimeOnly.MinValue),
                    EndDateTime = vac.EndDate.ToDateTime(TimeOnly.MaxValue),
                    StatusTypeId = activeTypeId,
                    Reason = "Vacaciones aprobadas"
                };
                await _db.EmployeeAvailabilityBlocks.AddAsync(block, ct);
                created++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return new SyncAvailabilityBlocksResultDto(created, updated, 0, new List<string> { $"Sincronización de vacaciones: {created} creados." });
    }

    public async Task<bool> HasBlockAsync(int employeeId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct) =>
        await _repo.HasActiveBlockAsync(employeeId, startDateTime, endDateTime, ct);
}

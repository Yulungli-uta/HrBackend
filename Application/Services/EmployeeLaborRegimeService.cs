using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.EmployeeLaborRegime;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Gestiona los regímenes laborales (LOSEP/LOES/CT) de un empleado, permitiendo
/// más de uno activo simultáneo (ej. nombramiento LOSEP + contrato ocasional LOES).
/// Calcula automáticamente cuál de los activos es el principal.
/// </summary>
public class EmployeeLaborRegimeService : IEmployeeLaborRegimeService
{
    private const string ContractTypeCategory = "CONTRACT_TYPE";
    private const string LosepName = "LOSEP";

    private readonly IEmployeeLaborRegimeRepository _repository;
    private readonly AppDbContext _db;

    public EmployeeLaborRegimeService(IEmployeeLaborRegimeRepository repository, AppDbContext db)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<List<EmployeeLaborRegimeDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
    {
        var items = await _repository.GetAllByEmployeeAsync(employeeId, ct);
        return await ToDtosAsync(items, ct);
    }

    public async Task<EmployeeLaborRegimeDto> CreateAsync(EmployeeLaborRegimeCreateDto dto, int? changedBy, CancellationToken ct = default)
    {
        var alreadyActive = await _db.EmployeeLaborRegimes
            .AnyAsync(r => r.EmployeeId == dto.EmployeeId && r.LaborRegimeId == dto.LaborRegimeId && r.IsActive, ct);

        if (alreadyActive)
            throw new InvalidOperationException("El empleado ya tiene ese régimen activo.");

        var entity = new EmployeeLaborRegime
        {
            EmployeeId = dto.EmployeeId,
            LaborRegimeId = dto.LaborRegimeId,
            DepartmentId = dto.DepartmentId,
            JobId = dto.JobId,
            IsIndefinite = dto.IsIndefinite,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            SourceContractId = dto.SourceContractId,
            SourcePersonnelActionId = dto.SourcePersonnelActionId,
            EffectiveFrom = dto.EffectiveFrom,
            IsActive = true,
            CreatedBy = changedBy,
            CreatedAt = DateTime.Now,
        };

        _db.EmployeeLaborRegimes.Add(entity);
        await _db.SaveChangesAsync(ct);

        await RecalculatePrincipalAsync(dto.EmployeeId, changedBy, ct);

        return (await ToDtosAsync([entity], ct)).Single();
    }

    public async Task<EmployeeLaborRegimeDto?> CloseAsync(int id, EmployeeLaborRegimeCloseDto dto, int? changedBy, CancellationToken ct = default)
    {
        var entity = await _db.EmployeeLaborRegimes.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return null;

        entity.IsActive = false;
        entity.EffectiveTo = dto.EffectiveTo;
        entity.IsPrincipal = false;
        entity.UpdatedBy = changedBy;
        entity.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(ct);

        await RecalculatePrincipalAsync(entity.EmployeeId, changedBy, ct);

        return (await ToDtosAsync([entity], ct)).Single();
    }

    /// <summary>
    /// Entre los regímenes activos del empleado: gana el nombramiento (IsIndefinite);
    /// si ninguno es nombramiento, gana LOSEP; si tampoco hay LOSEP activo, gana el más antiguo.
    /// </summary>
    private async Task RecalculatePrincipalAsync(int employeeId, int? changedBy, CancellationToken ct)
    {
        var active = await _db.EmployeeLaborRegimes
            .Where(r => r.EmployeeId == employeeId && r.IsActive)
            .ToListAsync(ct);

        if (active.Count == 0) return;

        EmployeeLaborRegime? principal = active
            .Where(r => r.IsIndefinite)
            .OrderBy(r => r.EffectiveFrom)
            .FirstOrDefault();

        if (principal is null)
        {
            var losepId = await _db.RefTypes
                .AsNoTracking()
                .Where(r => r.Category == ContractTypeCategory && r.Name == LosepName && r.IsActive)
                .Select(r => r.TypeId)
                .FirstOrDefaultAsync(ct);

            principal = (losepId != 0 ? active.Where(r => r.LaborRegimeId == losepId).OrderBy(r => r.EffectiveFrom).FirstOrDefault() : null)
                ?? active.OrderBy(r => r.EffectiveFrom).First();
        }

        foreach (var regime in active)
        {
            var shouldBePrincipal = regime.Id == principal.Id;
            if (regime.IsPrincipal == shouldBePrincipal) continue;

            regime.IsPrincipal = shouldBePrincipal;
            regime.UpdatedBy = changedBy;
            regime.UpdatedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<EmployeeLaborRegimeDto>> ToDtosAsync(List<EmployeeLaborRegime> items, CancellationToken ct)
    {
        if (items.Count == 0) return [];

        var employeeIds = items.Select(i => i.EmployeeId).Distinct().ToList();
        var regimeIds = items.Select(i => i.LaborRegimeId).Distinct().ToList();
        var departmentIds = items.Where(i => i.DepartmentId.HasValue).Select(i => i.DepartmentId!.Value).Distinct().ToList();
        var jobIds = items.Where(i => i.JobId.HasValue).Select(i => i.JobId!.Value).Distinct().ToList();

        var employees = await _db.Set<WsUtaSystem.Models.Views.VwEmployeeDetails>()
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.EmployeeID))
            .ToDictionaryAsync(e => e.EmployeeID, e => (Name: e.FirstName + " " + e.LastName, e.Email), ct);

        var regimeNames = await _db.RefTypes
            .AsNoTracking()
            .Where(r => regimeIds.Contains(r.TypeId))
            .ToDictionaryAsync(r => r.TypeId, r => r.Name, ct);

        var departmentNames = departmentIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Departments.AsNoTracking()
                .Where(d => departmentIds.Contains(d.DepartmentId))
                .ToDictionaryAsync(d => d.DepartmentId, d => d.Name, ct);

        var jobNames = jobIds.Count == 0
            ? new Dictionary<int, string?>()
            : await _db.Set<Job>().AsNoTracking()
                .Where(j => jobIds.Contains(j.JobID))
                .ToDictionaryAsync(j => j.JobID, j => j.Description, ct);

        return items.Select(i =>
        {
            employees.TryGetValue(i.EmployeeId, out var employee);
            regimeNames.TryGetValue(i.LaborRegimeId, out var regimeName);

            return new EmployeeLaborRegimeDto
            {
                Id = i.Id,
                EmployeeId = i.EmployeeId,
                EmployeeName = employee.Name,
                EmployeeEmail = employee.Email,
                LaborRegimeId = i.LaborRegimeId,
                LaborRegimeName = regimeName,
                DepartmentId = i.DepartmentId,
                DepartmentName = i.DepartmentId.HasValue && departmentNames.TryGetValue(i.DepartmentId.Value, out var dn) ? dn : null,
                JobId = i.JobId,
                JobName = i.JobId.HasValue && jobNames.TryGetValue(i.JobId.Value, out var jn) ? jn : null,
                IsIndefinite = i.IsIndefinite,
                DocumentType = i.DocumentType,
                DocumentNumber = i.DocumentNumber,
                SourceContractId = i.SourceContractId,
                SourcePersonnelActionId = i.SourcePersonnelActionId,
                EffectiveFrom = i.EffectiveFrom,
                EffectiveTo = i.EffectiveTo,
                IsActive = i.IsActive,
                IsPrincipal = i.IsPrincipal,
            };
        }).ToList();
    }
}

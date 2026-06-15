using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.ContractRequestPerson;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class ContractRequestPersonService
    : Service<ContractRequestPerson, int>, IContractRequestPersonService
{
    private const string PersonCategory    = "CONTRACT_REQUEST_PERSON_STATUS";
    private const string StatusPending     = "PENDIENTE";
    private const string StatusHired       = "CONTRATADO";
    private const string StatusInactive    = "INACTIVO";

    private const string TypeCategory      = "JOB_TYPE";
    private const string TypeDocente       = "DOCENTE";

    private const string SourceCategory    = "CONTRACT_REQUEST_PERSON_SOURCE";
    private const string SourceFromRequest = "REQUEST";
    private const string SourceAvailable   = "CONTRACT";

    private readonly IContractRequestPersonRepository _repo;
    private readonly AppDbContext _db;
    private readonly IRefTypesService _refTypes;

    public ContractRequestPersonService(
        IContractRequestPersonRepository repo,
        AppDbContext db,
        IRefTypesService refTypes) : base(repo)
    {
        _repo     = repo     ?? throw new ArgumentNullException(nameof(repo));
        _db       = db       ?? throw new ArgumentNullException(nameof(db));
        _refTypes = refTypes ?? throw new ArgumentNullException(nameof(refTypes));
    }

    public async Task<IEnumerable<ContractRequestPersonDto>> GetByRequestAsync(
        int requestId, CancellationToken ct = default)
    {
        var items = await _repo.GetByRequestAsync(requestId, ct);
        return await EnrichAsync(items, ct);
    }

    public async Task<IEnumerable<ContractRequestPersonDto>> GetPendingByRequestAsync(
        int requestId, CancellationToken ct = default)
    {
        var pendingId = await GetStatusIdAsync(StatusPending, ct);
        var items = await _repo.GetPendingByRequestAsync(requestId, pendingId, ct);
        return await EnrichAsync(items, ct);
    }

    public async Task<ContractRequestSlotsDto> GetSlotsAsync(int requestId, CancellationToken ct = default)
    {
        var request = await _db.Set<ContractRequest>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RequestId == requestId, ct)
            ?? throw new KeyNotFoundException($"ContractRequest id={requestId} no existe.");

        var pendingId = await GetStatusIdAsync(StatusPending, ct);
        var pendingCount = await _db.Set<ContractRequestPerson>()
            .AsNoTracking()
            .CountAsync(p => p.RequestId == requestId && p.StatusId == pendingId, ct);

        return new ContractRequestSlotsDto
        {
            RequestId            = requestId,
            NumberOfPeopleToHire = request.NumberOfPeopleToHire,
            TotalHired           = request.TotalPeopleHired,
            SlotsAvailable       = request.PendingCount,
            PendingPeople        = pendingCount
        };
    }

    public async Task<ContractRequestPersonDto> AddPersonAsync(
        int requestId, CreateContractRequestPersonDto dto, int createdBy, CancellationToken ct = default)
    {
        // Validar que la solicitud existe
        var requestExists = await _db.Set<ContractRequest>()
            .AnyAsync(r => r.RequestId == requestId, ct);
        if (!requestExists)
            throw new KeyNotFoundException($"ContractRequest id={requestId} no existe.");

        // Validar que el RequestPersonTypeId corresponde a la categoría JOB_TYPE
        var types = await _refTypes.GetByCategoryAsync(TypeCategory, ct);
        if (!types.Any(t => t.TypeId == dto.RequestPersonTypeId))
            throw new InvalidOperationException($"RequestPersonTypeId={dto.RequestPersonTypeId} no es un tipo válido de JOB_TYPE.");

        // Para DOCENTE: WeeklyClassHours y HourValue son obligatorios
        var typeName = types.First(t => t.TypeId == dto.RequestPersonTypeId).Name;
        if (typeName.Equals(TypeDocente, StringComparison.OrdinalIgnoreCase))
        {
            if (dto.WeeklyClassHours == null || dto.WeeklyClassHours <= 0)
                throw new InvalidOperationException("WeeklyClassHours es obligatorio para tipo DOCENTE.");
            if (dto.HourValue == null || dto.HourValue <= 0)
                throw new InvalidOperationException("HourValue es obligatorio para tipo DOCENTE.");
        }

        // Calcular MonthsPeriod y RMU
        var (months, rmu, rmuPeriod) = CalculateFinancials(
            dto.StartDate, dto.EndDate, dto.RequestPersonTypeId, typeName,
            dto.WeeklyClassHours, dto.HourValue, await GetJobRmuAsync(dto.JobId, ct));

        var pendingId   = await GetStatusIdAsync(StatusPending, ct);
        var sourceId    = await GetSourceIdAsync(SourceFromRequest, ct);

        var entity = new ContractRequestPerson
        {
            RequestId           = requestId,
            PersonId            = dto.PersonId,
            JobId               = dto.JobId,
            RequestPersonTypeId = dto.RequestPersonTypeId,
            StartDate           = dto.StartDate,
            EndDate             = dto.EndDate,
            WeeklyClassHours    = dto.WeeklyClassHours,
            HourValue           = dto.HourValue,
            MonthsPeriod        = months,
            Rmu                 = rmu,
            RmuPeriod           = rmuPeriod,
            EntrySourceId       = sourceId,
            IsHired             = false,
            StatusId            = pendingId,
            CreatedAt           = DateTime.Now,
            CreatedBy           = createdBy
        };

        var created = await base.CreateAsync(entity, ct);
        return await EnrichSingleAsync(created, ct);
    }

    public async Task UpdatePersonAsync(
        int requestPersonId, UpdateContractRequestPersonDto dto, int updatedBy, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var entity = await _db.Set<ContractRequestPerson>()
                .FirstOrDefaultAsync(p => p.RequestPersonId == requestPersonId, ct)
                ?? throw new KeyNotFoundException($"ContractRequestPerson id={requestPersonId} no existe.");

            if (entity.IsHired)
                throw new InvalidOperationException("No se puede modificar una persona ya contratada.");

            var types    = await _refTypes.GetByCategoryAsync(TypeCategory, ct);
            var typeName = types.FirstOrDefault(t => t.TypeId == dto.RequestPersonTypeId)?.Name ?? "";

            if (typeName.Equals(TypeDocente, StringComparison.OrdinalIgnoreCase))
            {
                if (dto.WeeklyClassHours == null || dto.WeeklyClassHours <= 0)
                    throw new InvalidOperationException("WeeklyClassHours es obligatorio para tipo DOCENTE.");
                if (dto.HourValue == null || dto.HourValue <= 0)
                    throw new InvalidOperationException("HourValue es obligatorio para tipo DOCENTE.");
            }

            var jobRmu = await GetJobRmuAsync(dto.JobId, ct);
            var (months, rmu, rmuPeriod) = CalculateFinancials(
                dto.StartDate, dto.EndDate, dto.RequestPersonTypeId, typeName,
                dto.WeeklyClassHours, dto.HourValue, jobRmu);

            entity.PersonId            = dto.PersonId;
            entity.JobId               = dto.JobId;
            entity.RequestPersonTypeId = dto.RequestPersonTypeId;
            entity.StartDate           = dto.StartDate;
            entity.EndDate             = dto.EndDate;
            entity.WeeklyClassHours    = dto.WeeklyClassHours;
            entity.HourValue           = dto.HourValue;
            entity.MonthsPeriod        = months;
            entity.Rmu                 = rmu;
            entity.RmuPeriod           = rmuPeriod;
            entity.UpdatedAt           = DateTime.Now;
            entity.UpdatedBy           = updatedBy;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task HireAsync(int requestPersonId, int contractId, int updatedBy, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var entity = await _db.Set<ContractRequestPerson>()
                .FirstOrDefaultAsync(p => p.RequestPersonId == requestPersonId, ct)
                ?? throw new KeyNotFoundException($"ContractRequestPerson id={requestPersonId} no existe.");

            var hiredId = await GetStatusIdAsync(StatusHired, ct);

            entity.IsHired   = true;
            entity.ContractId = contractId;
            entity.StatusId   = hiredId;
            entity.UpdatedAt  = DateTime.Now;
            entity.UpdatedBy  = updatedBy;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task RecordHiredFromAvailableAsync(
        int requestId, int personId, int jobId, int contractId, int createdBy, CancellationToken ct = default)
    {
        var requestExists = await _db.Set<ContractRequest>()
            .AnyAsync(r => r.RequestId == requestId, ct);
        if (!requestExists)
            throw new KeyNotFoundException($"ContractRequest id={requestId} no existe.");

        var hiredId  = await GetStatusIdAsync(StatusHired, ct);
        var sourceId = await GetSourceIdAsync(SourceAvailable, ct);

        // Si ya existe un registro activo para (requestId, personId), actualizarlo en vez de insertar
        var existing = await _db.Set<ContractRequestPerson>()
            .FirstOrDefaultAsync(x => x.RequestId == requestId && x.PersonId == personId, ct);

        if (existing is not null)
        {
            existing.JobId      = jobId;
            existing.ContractId = contractId;
            existing.IsHired    = true;
            existing.StatusId   = hiredId;
            existing.UpdatedAt  = DateTime.Now;
            existing.UpdatedBy  = createdBy;
            await _db.SaveChangesAsync(ct);
            return;
        }

        // Obtener el tipo ADMINISTRATIVO por defecto (o el primer tipo disponible)
        var types = await _refTypes.GetByCategoryAsync(TypeCategory, ct);
        var defaultTypeId = types.FirstOrDefault()?.TypeId ?? 0;

        var entity = new ContractRequestPerson
        {
            RequestId           = requestId,
            PersonId            = personId,
            JobId               = jobId,
            RequestPersonTypeId = defaultTypeId,
            EntrySourceId       = sourceId,
            IsHired             = true,
            ContractId          = contractId,
            StatusId            = hiredId,
            CreatedAt           = DateTime.Now,
            CreatedBy           = createdBy
        };

        await base.CreateAsync(entity, ct);
    }

    public async Task InactivateByContractAsync(int contractId, int updatedBy, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var inactiveId = await GetStatusIdAsync(StatusInactive, ct);

            var persons = await _db.Set<ContractRequestPerson>()
                .Where(p => p.ContractId == contractId)
                .ToListAsync(ct);

            foreach (var p in persons)
            {
                p.IsHired    = false;
                p.StatusId   = inactiveId;
                p.UpdatedAt  = DateTime.Now;
                p.UpdatedBy  = updatedBy;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    // ── Helpers ──────────────────────────────────────────────

    private async Task<int> GetStatusIdAsync(string name, CancellationToken ct)
    {
        var statuses = await _refTypes.GetByCategoryAsync(PersonCategory, ct);
        return statuses.FirstOrDefault(x => x.Name == name)?.TypeId
            ?? throw new InvalidOperationException($"Estado '{PersonCategory}/{name}' no existe en ref_Types.");
    }

    private async Task<int> GetSourceIdAsync(string name, CancellationToken ct)
    {
        var sources = await _refTypes.GetByCategoryAsync(SourceCategory, ct);
        return sources.FirstOrDefault(x => x.Name == name)?.TypeId
            ?? throw new InvalidOperationException($"Fuente '{SourceCategory}/{name}' no existe en ref_Types.");
    }

    private async Task<decimal?> GetJobRmuAsync(int jobId, CancellationToken ct)
    {
        // El modelo Job no tiene Rmu en la entidad base; se usa SalaryHistory o se retorna null
        // Por ahora retornamos null y el frontend enviará el valor calculado
        await Task.CompletedTask;
        return null;
    }

    private static (decimal? months, decimal? rmu, decimal? rmuPeriod) CalculateFinancials(
        DateTime? startDate, DateTime? endDate, int typeId, string typeName,
        decimal? weeklyHours, decimal? hourValue, decimal? jobRmu)
    {
        decimal? months = null;
        if (startDate.HasValue && endDate.HasValue && endDate >= startDate)
        {
            var diffMs = (endDate.Value - startDate.Value).TotalMilliseconds;
            months = (decimal)(diffMs / (1000.0 * 60 * 60 * 24 * 30));
        }

        decimal? rmu = null;
        if (typeName.Equals("DOCENTE", StringComparison.OrdinalIgnoreCase))
        {
            if (weeklyHours.HasValue && hourValue.HasValue)
                rmu = weeklyHours.Value * hourValue.Value * 4;
        }
        else
        {
            rmu = jobRmu;
        }

        decimal? rmuPeriod = null;
        if (rmu.HasValue && months.HasValue)
            rmuPeriod = rmu.Value * months.Value;

        return (months, rmu, rmuPeriod);
    }

    private async Task<IEnumerable<ContractRequestPersonDto>> EnrichAsync(
        IEnumerable<ContractRequestPerson> items, CancellationToken ct)
    {
        var typeMap   = (await _refTypes.GetByCategoryAsync(TypeCategory, ct))
                            .ToDictionary(x => x.TypeId, x => x.Name);
        var statusMap = (await _refTypes.GetByCategoryAsync(PersonCategory, ct))
                            .ToDictionary(x => x.TypeId, x => x.Name);
        var sourceMap = (await _refTypes.GetByCategoryAsync(SourceCategory, ct))
                            .ToDictionary(x => x.TypeId, x => x.Name);

        return items.Select(p => MapToDto(p, typeMap, statusMap, sourceMap)).ToList();
    }

    private async Task<ContractRequestPersonDto> EnrichSingleAsync(
        ContractRequestPerson item, CancellationToken ct)
    {
        var enriched = await EnrichAsync([item], ct);
        return enriched.First();
    }

    private static ContractRequestPersonDto MapToDto(
        ContractRequestPerson p,
        Dictionary<int, string> typeMap,
        Dictionary<int, string> statusMap,
        Dictionary<int, string> sourceMap) => new()
    {
        RequestPersonId      = p.RequestPersonId,
        RequestId            = p.RequestId,
        PersonId             = p.PersonId,
        PersonFullName       = p.Person != null
                               ? $"{p.Person.FirstName} {p.Person.LastName}".Trim()
                               : null,
        PersonIdentification = p.Person?.IdCard,
        JobId                = p.JobId,
        JobName              = p.Job?.Description,
        RequestPersonTypeId  = p.RequestPersonTypeId,
        RequestPersonTypeName = typeMap.TryGetValue(p.RequestPersonTypeId, out var tn) ? tn : null,
        StartDate            = p.StartDate,
        EndDate              = p.EndDate,
        WeeklyClassHours     = p.WeeklyClassHours,
        HourValue            = p.HourValue,
        MonthsPeriod         = p.MonthsPeriod,
        Rmu                  = p.Rmu,
        RmuPeriod            = p.RmuPeriod,
        EntrySourceId        = p.EntrySourceId,
        EntrySourceName      = p.EntrySourceId.HasValue && sourceMap.TryGetValue(p.EntrySourceId.Value, out var sn) ? sn : null,
        IsHired              = p.IsHired,
        ContractId           = p.ContractId,
        StatusId             = p.StatusId,
        StatusName           = p.StatusId.HasValue && statusMap.TryGetValue(p.StatusId.Value, out var st) ? st : null,
        CreatedAt            = p.CreatedAt,
        CreatedBy            = p.CreatedBy,
        UpdatedAt            = p.UpdatedAt,
        UpdatedBy            = p.UpdatedBy
    };
}

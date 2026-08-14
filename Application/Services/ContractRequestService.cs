using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Extensions;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.ContractRequest;
using WsUtaSystem.Application.DTOs.ContractRequestPerson;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class ContractRequestService : Service<ContractRequest, int>, IContractRequestService
{
    private const string StatusCategory       = "CONTRACT_REQUEST_STATUS";
    private const string StatusInitial        = "PENDIENTE_CERT_FINANCIERA";
    private const string StatusEnProceso      = "EN_PROCESO";
    private const string StatusCompletado     = "COMPLETADO";
    private const string StatusPendingCorrec  = "PENDIENTE_CORRECCION";

    private const string PersonStatusCategory = "CONTRACT_REQUEST_PERSON_STATUS";
    private const string PersonStatusPending  = "PENDIENTE";

    private readonly IContractRequestRepository _repo;
    private readonly AppDbContext _db;
    private readonly IRefTypesService _refTypes;

    public ContractRequestService(
        IContractRequestRepository repo,
        AppDbContext db,
        IRefTypesService refTypes
    ) : base(repo)
    {
        _repo     = repo     ?? throw new ArgumentNullException(nameof(repo));
        _db       = db       ?? throw new ArgumentNullException(nameof(db));
        _refTypes = refTypes ?? throw new ArgumentNullException(nameof(refTypes));
    }

    public new async Task<ContractRequest> CreateAsync(ContractRequest entity, CancellationToken ct)
    {
        entity.Status = await GetStatusIdAsync(StatusInitial, ct);
        entity.TotalPeopleHired = 0;
        return await base.CreateAsync(entity, ct);
    }

    public async Task<PagedContractRequestResult> GetPagedAsync(ContractRequestQueryFilter filter, CancellationToken ct = default)
    {
        var allStatuses = (await _refTypes.GetByCategoryAsync(StatusCategory, ct)).ToList();
        var statusMap   = allStatuses.ToDictionary(x => x.TypeId, x => x.Name);

        // Resolver TypeId del filtro de estado si viene
        int? filterStatusId = null;
        if (!string.IsNullOrWhiteSpace(filter.StatusName))
        {
            filterStatusId = allStatuses.FirstOrDefault(x => x.Name == filter.StatusName)?.TypeId;
            if (filterStatusId is null)
                return new PagedContractRequestResult([], 0, filter.Page, filter.PageSize, 0);
        }

        var page     = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var paged = await base.GetPagedAsync(
            predicate: r =>
                (filterStatusId == null || r.Status == filterStatusId) &&
                (filter.DepartmentId == null || r.DepartmentId == filter.DepartmentId) &&
                (filter.WorkModalityId == null || r.WorkModalityId == filter.WorkModalityId) &&
                (filter.Search == null || (r.Observation != null && r.Observation.Contains(filter.Search))),
            page:     page,
            pageSize: pageSize,
            ct:       ct,
            orderBy:  r => r.RequestId,
            ascending: false);

        var items = paged.Items.Select(r => MapToDto(r, statusMap)).ToList();

        return new PagedContractRequestResult(
            items,
            (int)paged.TotalCount,
            paged.Page,
            paged.PageSize,
            paged.TotalPages);
    }

    public async Task<IEnumerable<ContractRequestDto>> GetByStatusAsync(string statusName, CancellationToken ct = default)
    {
        var allStatuses = (await _refTypes.GetByCategoryAsync(StatusCategory, ct)).ToList();
        var statusMap   = allStatuses.ToDictionary(x => x.TypeId, x => x.Name);

        var target = allStatuses.FirstOrDefault(x => x.Name == statusName);
        if (target is null) return [];

        var items = await _repo.GetByStatusAsync(target.TypeId, ct);

        return items.Select(r => MapToDto(r, statusMap)).ToList();
    }

    public async Task<int> GetPendingCountAsync(int requestId, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"ContractRequest id={requestId} no existe.");
        return entity.PendingCount;
    }

    public async Task IncrementTotalHiredAsync(int requestId, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var entity = await _db.Set<ContractRequest>()
                .FirstOrDefaultAsync(x => x.RequestId == requestId, ct)
                ?? throw new KeyNotFoundException($"ContractRequest id={requestId} no existe.");

            entity.TotalPeopleHired++;
            entity.UpdatedAt = DateTime.Now;

            // Transición de estado según cupo
            if (entity.TotalPeopleHired >= entity.NumberOfPeopleToHire)
                entity.Status = await GetStatusIdAsync(StatusCompletado, ct);
            else
                entity.Status = await GetStatusIdAsync(StatusEnProceso, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task<ContractRequestSlotsDto> GetSlotsAsync(int requestId, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"ContractRequest id={requestId} no existe.");

        var personStatuses = await _refTypes.GetByCategoryAsync(PersonStatusCategory, ct);
        var pendingPersonId = personStatuses.FirstOrDefault(x => x.Name == PersonStatusPending)?.TypeId;

        var pendingCount = pendingPersonId.HasValue
            ? await _db.Set<ContractRequestPerson>()
                .AsNoTracking()
                .CountAsync(p => p.RequestId == requestId && p.StatusId == pendingPersonId.Value, ct)
            : 0;

        return new ContractRequestSlotsDto
        {
            RequestId            = requestId,
            NumberOfPeopleToHire = entity.NumberOfPeopleToHire,
            TotalHired           = entity.TotalPeopleHired,
            SlotsAvailable       = entity.PendingCount,
            PendingPeople        = pendingCount
        };
    }

    public async Task<IEnumerable<AvailablePersonDto>> SearchAvailablePeopleAsync(
        int requestId, string? search, CancellationToken ct = default)
    {
        var query = _db.Set<People>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                (p.FirstName != null && p.FirstName.Contains(term)) ||
                (p.LastName  != null && p.LastName.Contains(term))  ||
                (p.IdCard    != null && p.IdCard.Contains(term)));
        }

        var people = await query
            .OrderBy(p => p.LastName)
            .Take(50)
            .ToListAsync(ct);

        return people.Select(p => new AvailablePersonDto
        {
            PersonId       = p.PersonId,
            FullName       = p.GetFullName(),
            Identification = p.IdCard
        }).ToList();
    }

    public async Task SendToCorrectionAsync(int requestId, string reason, int userId, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var entity = await _db.Set<ContractRequest>()
                .FirstOrDefaultAsync(x => x.RequestId == requestId, ct)
                ?? throw new KeyNotFoundException($"ContractRequest id={requestId} no existe.");

            entity.Status                  = await GetStatusIdAsync(StatusPendingCorrec, ct);
            entity.PendingCorrectionReason = reason;
            entity.UpdatedAt               = DateTime.Now;
            entity.UpdatedBy               = userId;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    // ── Helpers ──────────────────────────────────────────────

    private async Task<int> GetStatusIdAsync(string name, CancellationToken ct)
    {
        var statuses = await _refTypes.GetByCategoryAsync(StatusCategory, ct);
        return statuses.FirstOrDefault(x => x.Name == name)?.TypeId
            ?? throw new InvalidOperationException($"Estado '{StatusCategory}/{name}' no existe en ref_Types.");
    }

    private static ContractRequestDto MapToDto(ContractRequest r, Dictionary<int, string> statusMap) => new()
    {
        RequestId                = r.RequestId,
        DepartmentId             = r.DepartmentId,
        WorkModalityId           = r.WorkModalityId,
        NumberOfPeopleToHire     = r.NumberOfPeopleToHire,
        NumberHour               = r.NumberHour,
        TotalPeopleHired         = r.TotalPeopleHired,
        Observation              = r.Observation,
        StartDate                = r.StartDate,
        EndDate                  = r.EndDate,
        PendingCorrectionReason  = r.PendingCorrectionReason,
        CreatedAt                = r.CreatedAt ?? DateTime.MinValue,
        CreatedBy                = r.CreatedBy ?? 0,
        UpdatedAt                = r.UpdatedAt,
        UpdatedBy                = r.UpdatedBy,
        Status                   = r.Status,
        PendingCount             = r.PendingCount,
        StatusName               = r.Status.HasValue && statusMap.TryGetValue(r.Status.Value, out var n) ? n : null
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractRequestReportDto>> GetForReportAsync(ReportFilterDto filter, CancellationToken ct = default)
    {
        var statusMap = await _db.RefTypes.AsNoTracking()
            .Where(x => x.Category == StatusCategory)
            .ToDictionaryAsync(x => x.TypeId, x => x.Name, ct);

        var wmMap = await _db.RefTypes.AsNoTracking()
            .Where(x => x.Category == "WORK_MODALITY")
            .ToDictionaryAsync(x => x.TypeId, x => x.Name, ct);

        var query =
            from r in _db.ContractRequest.AsNoTracking()
            join d in _db.Departments.AsNoTracking() on r.DepartmentId equals d.DepartmentId into dg
            from d in dg.DefaultIfEmpty()
            where (!filter.StartDate.HasValue || (r.StartDate.HasValue && r.StartDate >= filter.StartDate.Value))
               && (!filter.EndDate.HasValue   || (r.StartDate.HasValue && r.StartDate <= filter.EndDate.Value))
               && (string.IsNullOrEmpty(filter.Status) ||
                   (r.Status.HasValue && statusMap.ContainsKey(r.Status.Value) && statusMap[r.Status.Value] == filter.Status))
               && (!filter.DepartmentId.HasValue || r.DepartmentId == filter.DepartmentId.Value)
            orderby r.CreatedAt descending
            select new ContractRequestReportDto
            {
                RequestId            = r.RequestId,
                DepartmentName       = d != null ? d.Name : "—",
                WorkModalityName     = r.WorkModalityId.HasValue && wmMap.ContainsKey(r.WorkModalityId.Value)
                                            ? wmMap[r.WorkModalityId.Value] : null,
                NumberHour           = r.NumberHour,
                NumberOfPeopleToHire = r.NumberOfPeopleToHire,
                TotalPeopleHired     = r.TotalPeopleHired,
                PendingCount         = r.NumberOfPeopleToHire - r.TotalPeopleHired > 0
                                            ? r.NumberOfPeopleToHire - r.TotalPeopleHired : 0,
                StartDate            = r.StartDate,
                EndDate              = r.EndDate,
                StatusName           = r.Status.HasValue && statusMap.ContainsKey(r.Status.Value)
                                            ? statusMap[r.Status.Value] : "—",
                Observation          = r.Observation,
                CreatedAt            = r.CreatedAt ?? DateTime.MinValue
            };

        return await query.ToListAsync(ct);
    }
}

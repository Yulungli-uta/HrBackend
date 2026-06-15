using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.FinancialCertification;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class FinancialCertificationService : Service<FinancialCertification, int>, IFinancialCertificationService
{
    private const string CertCategory         = "FIN_CERT_STATUS";
    private const string CertPending          = "PENDIENTE_REVISION";
    private const string CertApproved         = "APROBADA";
    private const string CertRejected         = "RECHAZADA";
    private const string CertPendingCorrection = "PENDIENTE_CORRECCION";

    private const string RequestCategory              = "CONTRACT_REQUEST_STATUS";
    private const string RequestPendingHiring         = "PENDIENTE_CONTRATACION";
    private const string RequestCertRejected          = "CERT_RECHAZADA";
    private const string RequestPendingCorrection     = "PENDIENTE_CORRECCION";
    private const string RequestPendingCertFinanciera = "PENDIENTE_CERT_FINANCIERA";

    private const string RejectionTypeCategory = "FIN_CERT_REJECTION_TYPE";
    private const string RejectionTypeTemporary = "TEMPORAL";
    private const string RejectionTypeDefinitive = "DEFINITIVO";

    private readonly IFinancialCertificationRepository _repo;
    private readonly AppDbContext _db;
    private readonly IRefTypesService _refTypes;

    public FinancialCertificationService(
        IFinancialCertificationRepository repo,
        AppDbContext db,
        IRefTypesService refTypes
    ) : base(repo)
    {
        _repo     = repo     ?? throw new ArgumentNullException(nameof(repo));
        _db       = db       ?? throw new ArgumentNullException(nameof(db));
        _refTypes = refTypes ?? throw new ArgumentNullException(nameof(refTypes));
    }

    public new async Task<FinancialCertification> CreateAsync(FinancialCertification entity, CancellationToken ct)
    {
        // Validar que la solicitud existe si se vincula
        if (entity.RequestId.HasValue)
        {
            var exists = await _db.Set<ContractRequest>()
                .AnyAsync(x => x.RequestId == entity.RequestId.Value, ct);
            if (!exists)
                throw new KeyNotFoundException($"ContractRequest id={entity.RequestId} no existe.");
        }

        entity.Status = await GetCertStatusIdAsync(CertPending, ct);
        return await base.CreateAsync(entity, ct);
    }

    public async Task<PagedFinancialCertificationResult> GetPagedAsync(FinancialCertificationQueryFilter filter, CancellationToken ct = default)
    {
        var certStatuses = (await _refTypes.GetByCategoryAsync(CertCategory, ct)).ToList();
        var statusMap    = certStatuses.ToDictionary(x => x.TypeId, x => x.Name);

        int? filterStatusId = null;
        if (!string.IsNullOrWhiteSpace(filter.StatusName))
        {
            filterStatusId = certStatuses.FirstOrDefault(x => x.Name == filter.StatusName)?.TypeId;
            if (filterStatusId is null)
                return new PagedFinancialCertificationResult([], 0, filter.Page, filter.PageSize, 0);
        }

        var page     = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var paged = await base.GetPagedAsync(
            predicate: c =>
                (filterStatusId == null   || c.Status == filterStatusId) &&
                (filter.RequestId == null || c.RequestId == filter.RequestId) &&
                (filter.CertCode  == null || c.CertCode.Contains(filter.CertCode)) &&
                (filter.Search    == null || c.CertCode.Contains(filter.Search) ||
                                            (c.CertNumber != null && c.CertNumber.Contains(filter.Search))),
            page:     page,
            pageSize: pageSize,
            ct:       ct,
            orderBy:  c => c.CertificationId,
            ascending: false);

        // Enriquecer con RequestSummary
        var requestIds = paged.Items
            .Where(c => c.RequestId.HasValue)
            .Select(c => c.RequestId!.Value)
            .Distinct()
            .ToList();

        var requests = requestIds.Count > 0
            ? await _db.Set<ContractRequest>()
                .AsNoTracking()
                .Where(r => requestIds.Contains(r.RequestId))
                .ToDictionaryAsync(r => r.RequestId, ct)
            : [];

        var items = paged.Items.Select(c => new FinancialCertificationDto
        {
            CertificationId = c.CertificationId,
            RequestId       = c.RequestId,
            CertCode        = c.CertCode,
            CertNumber      = c.CertNumber,
            Budget          = c.Budget,
            CertBudgetDate  = c.CertBudgetDate,
            RmuHour         = c.RmuHour,
            RmuCon          = c.RmuCon,
            FileName        = c.FileName,
            FilePath        = c.FilePath,
            CreatedAt       = c.CreatedAt,
            CreatedBy       = c.CreatedBy,
            UpdatedAt       = c.UpdatedAt,
            UpdatedBy       = c.UpdatedBy,
            Status          = c.Status,
            RejectionReason = c.RejectionReason,
            RejectedAt      = c.RejectedAt,
            RejectedBy      = c.RejectedBy,
            RejectionTypeId = c.RejectionTypeId,
            StatusName      = c.Status.HasValue && statusMap.TryGetValue(c.Status.Value, out var n) ? n : null,
            RequestSummary  = c.RequestId.HasValue && requests.TryGetValue(c.RequestId.Value, out var req)
                ? new ContractRequestSummary
                {
                    RequestId            = req.RequestId,
                    NumberOfPeopleToHire = req.NumberOfPeopleToHire,
                    TotalPeopleHired     = req.TotalPeopleHired,
                    PendingCount         = req.PendingCount
                }
                : null
        }).ToList();

        return new PagedFinancialCertificationResult(
            items,
            (int)paged.TotalCount,
            paged.Page,
            paged.PageSize,
            paged.TotalPages);
    }

    public async Task<IEnumerable<FinancialCertificationDto>> GetPendingAsync(CancellationToken ct = default)
    {
        var pendingId = await GetCertStatusIdAsync(CertPending, ct);
        var items     = await _repo.GetByStatusAsync(pendingId, ct);
        return await EnrichAsync(items, ct);
    }

    public async Task ApproveAsync(int certificationId, int userId, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var cert = await _db.Set<FinancialCertification>()
                .FirstOrDefaultAsync(x => x.CertificationId == certificationId, ct)
                ?? throw new KeyNotFoundException($"Certificación id={certificationId} no existe.");

            cert.Status    = await GetCertStatusIdAsync(CertApproved, ct);
            cert.UpdatedAt = DateTime.Now;
            cert.UpdatedBy = userId;

            // Actualizar solicitud → PENDIENTE_CONTRATACION
            if (cert.RequestId.HasValue)
            {
                var request = await _db.Set<ContractRequest>()
                    .FirstOrDefaultAsync(x => x.RequestId == cert.RequestId.Value, ct);

                if (request is not null)
                {
                    request.Status    = await GetRequestStatusIdAsync(RequestPendingHiring, ct);
                    request.UpdatedAt = DateTime.Now;
                    request.UpdatedBy = userId;
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task RejectAsync(int certificationId, string? reason, int userId, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var cert = await _db.Set<FinancialCertification>()
                .FirstOrDefaultAsync(x => x.CertificationId == certificationId, ct)
                ?? throw new KeyNotFoundException($"Certificación id={certificationId} no existe.");

            cert.Status    = await GetCertStatusIdAsync(CertRejected, ct);
            cert.UpdatedAt = DateTime.Now;
            cert.UpdatedBy = userId;

            // Actualizar solicitud → CERT_RECHAZADA
            if (cert.RequestId.HasValue)
            {
                var request = await _db.Set<ContractRequest>()
                    .FirstOrDefaultAsync(x => x.RequestId == cert.RequestId.Value, ct);

                if (request is not null)
                {
                    request.Status    = await GetRequestStatusIdAsync(RequestCertRejected, ct);
                    request.UpdatedAt = DateTime.Now;
                    request.UpdatedBy = userId;
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task RejectTemporaryAsync(int certificationId, string? reason, int userId, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var cert = await _db.Set<FinancialCertification>()
                .FirstOrDefaultAsync(x => x.CertificationId == certificationId, ct)
                ?? throw new KeyNotFoundException($"Certificación id={certificationId} no existe.");

            var rejTypeId = await GetRejectionTypeIdAsync(RejectionTypeTemporary, ct);

            cert.Status          = await GetCertStatusIdAsync(CertPendingCorrection, ct);
            cert.RejectionReason = reason;
            cert.RejectedAt      = DateTime.Now;
            cert.RejectedBy      = userId;
            cert.RejectionTypeId = rejTypeId;
            cert.UpdatedAt       = DateTime.Now;
            cert.UpdatedBy       = userId;

            // Registrar en historial
            _db.Set<FinancialCertificationRejectionHistory>().Add(new FinancialCertificationRejectionHistory
            {
                CertificationId = certificationId,
                RejectionTypeId = rejTypeId,
                RejectionReason = reason,
                RejectedAt      = DateTime.Now,
                RejectedBy      = userId
            });

            // Actualizar solicitud → PENDIENTE_CORRECCION
            if (cert.RequestId.HasValue)
            {
                var request = await _db.Set<ContractRequest>()
                    .FirstOrDefaultAsync(x => x.RequestId == cert.RequestId.Value, ct);

                if (request is not null)
                {
                    request.Status               = await GetRequestStatusIdAsync(RequestPendingCorrection, ct);
                    request.PendingCorrectionReason = reason;
                    request.UpdatedAt            = DateTime.Now;
                    request.UpdatedBy            = userId;
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task ResendAsync(int certificationId, int userId, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var cert = await _db.Set<FinancialCertification>()
                .FirstOrDefaultAsync(x => x.CertificationId == certificationId, ct)
                ?? throw new KeyNotFoundException($"Certificación id={certificationId} no existe.");

            if (cert.Status != await GetCertStatusIdAsync(CertPendingCorrection, ct))
                throw new InvalidOperationException("Solo se puede reenviar una certificación en estado PENDIENTE_CORRECCION.");

            cert.Status          = await GetCertStatusIdAsync(CertPending, ct);
            cert.RejectionReason = null;
            cert.RejectedAt      = null;
            cert.RejectedBy      = null;
            cert.RejectionTypeId = null;
            cert.UpdatedAt       = DateTime.Now;
            cert.UpdatedBy       = userId;

            // Actualizar solicitud → PENDIENTE_CERT_FINANCIERA
            if (cert.RequestId.HasValue)
            {
                var request = await _db.Set<ContractRequest>()
                    .FirstOrDefaultAsync(x => x.RequestId == cert.RequestId.Value, ct);

                if (request is not null)
                {
                    request.Status                  = await GetRequestStatusIdAsync(RequestPendingCertFinanciera, ct);
                    request.PendingCorrectionReason = null;
                    request.UpdatedAt               = DateTime.Now;
                    request.UpdatedBy               = userId;
                }
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task<FinancialCertificationDto?> GetDetailAsync(int certificationId, CancellationToken ct = default)
    {
        var cert = await _db.Set<FinancialCertification>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CertificationId == certificationId, ct);

        if (cert is null) return null;

        var enriched = await EnrichAsync([cert], ct);
        return enriched.FirstOrDefault();
    }

    // ── Helpers ──────────────────────────────────────────────

    private async Task<int> GetCertStatusIdAsync(string name, CancellationToken ct)
    {
        var statuses = await _refTypes.GetByCategoryAsync(CertCategory, ct);
        return statuses.FirstOrDefault(x => x.Name == name)?.TypeId
            ?? throw new InvalidOperationException($"Estado '{CertCategory}/{name}' no existe en ref_Types.");
    }

    private async Task<int?> GetRejectionTypeIdAsync(string name, CancellationToken ct)
    {
        var types = await _refTypes.GetByCategoryAsync(RejectionTypeCategory, ct);
        return types.FirstOrDefault(x => x.Name == name)?.TypeId;
    }

    private async Task<int> GetRequestStatusIdAsync(string name, CancellationToken ct)
    {
        var statuses = await _refTypes.GetByCategoryAsync(RequestCategory, ct);
        return statuses.FirstOrDefault(x => x.Name == name)?.TypeId
            ?? throw new InvalidOperationException($"Estado '{RequestCategory}/{name}' no existe en ref_Types.");
    }

    private async Task<IEnumerable<FinancialCertificationDto>> EnrichAsync(
        IEnumerable<FinancialCertification> items, CancellationToken ct)
    {
        var certStatuses = (await _refTypes.GetByCategoryAsync(CertCategory, ct))
            .ToDictionary(x => x.TypeId, x => x.Name);

        var requestIds = items
            .Where(x => x.RequestId.HasValue)
            .Select(x => x.RequestId!.Value)
            .Distinct()
            .ToList();

        var requests = requestIds.Count > 0
            ? await _db.Set<ContractRequest>()
                .AsNoTracking()
                .Where(r => requestIds.Contains(r.RequestId))
                .ToDictionaryAsync(r => r.RequestId, ct)
            : [];

        return items.Select(c => new FinancialCertificationDto
        {
            CertificationId = c.CertificationId,
            RequestId       = c.RequestId,
            CertCode        = c.CertCode,
            CertNumber      = c.CertNumber,
            Budget          = c.Budget,
            CertBudgetDate  = c.CertBudgetDate,
            RmuHour         = c.RmuHour,
            RmuCon          = c.RmuCon,
            FileName        = c.FileName,
            FilePath        = c.FilePath,
            CreatedAt       = c.CreatedAt,
            CreatedBy       = c.CreatedBy,
            UpdatedAt       = c.UpdatedAt,
            UpdatedBy       = c.UpdatedBy,
            Status          = c.Status,
            RejectionReason = c.RejectionReason,
            RejectedAt      = c.RejectedAt,
            RejectedBy      = c.RejectedBy,
            RejectionTypeId = c.RejectionTypeId,
            StatusName      = c.Status.HasValue && certStatuses.TryGetValue(c.Status.Value, out var n) ? n : null,
            RequestSummary  = c.RequestId.HasValue && requests.TryGetValue(c.RequestId.Value, out var req)
                ? new ContractRequestSummary
                {
                    RequestId            = req.RequestId,
                    NumberOfPeopleToHire = req.NumberOfPeopleToHire,
                    TotalPeopleHired     = req.TotalPeopleHired,
                    PendingCount         = req.PendingCount
                }
                : null
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CertificationReportDto>> GetForReportAsync(ReportFilterDto filter, CancellationToken ct = default)
    {
        var statusMap = await _db.RefTypes.AsNoTracking()
            .Where(x => x.Category == CertCategory)
            .ToDictionaryAsync(x => x.TypeId, x => x.Name, ct);

        var query =
            from cert in _db.FinancialCertification.AsNoTracking()
            join req  in _db.ContractRequest.AsNoTracking()  on cert.RequestId   equals req.RequestId into rg
            from req in rg.DefaultIfEmpty()
            join d    in _db.Departments.AsNoTracking()       on req.DepartmentId equals d.DepartmentId into dg
            from d in dg.DefaultIfEmpty()
            where (!filter.StartDate.HasValue || (cert.CertBudgetDate.HasValue && cert.CertBudgetDate >= filter.StartDate.Value))
               && (!filter.EndDate.HasValue   || (cert.CertBudgetDate.HasValue && cert.CertBudgetDate <= filter.EndDate.Value))
               && (string.IsNullOrEmpty(filter.Status) ||
                   (cert.Status.HasValue && statusMap.ContainsKey(cert.Status.Value) && statusMap[cert.Status.Value] == filter.Status))
            orderby cert.CertBudgetDate descending
            select new CertificationReportDto
            {
                CertificationId        = cert.CertificationId,
                CertCode               = cert.CertCode,
                CertNumber             = cert.CertNumber,
                Budget                 = cert.Budget,
                RmuHour                = cert.RmuHour,
                RmuCon                 = cert.RmuCon,
                CertBudgetDate         = cert.CertBudgetDate,
                StatusName             = cert.Status.HasValue && statusMap.ContainsKey(cert.Status.Value)
                                              ? statusMap[cert.Status.Value] : "—",
                RequestId              = cert.RequestId,
                DepartmentName         = d != null ? d.Name : "—",
                NumberOfPeopleRequested = req != null ? req.NumberOfPeopleToHire : null,
                RejectionReason        = cert.RejectionReason,
                CreatedAt              = cert.CreatedAt
            };

        return await query.ToListAsync(ct);
    }
}

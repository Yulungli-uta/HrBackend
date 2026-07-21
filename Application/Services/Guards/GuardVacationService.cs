using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class GuardVacationService : IGuardVacationService
{
    private readonly IGuardVacationPlanRepository _planRepo;
    private readonly IGuardVacationRequestRepository _requestRepo;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GuardVacationService(
        IGuardVacationPlanRepository planRepo,
        IGuardVacationRequestRepository requestRepo,
        AppDbContext db,
        ICurrentUserService currentUser)
    {
        _planRepo = planRepo;
        _requestRepo = requestRepo;
        _db = db;
        _currentUser = currentUser;
    }

    // ─── Planes de vacaciones ─────────────────────────────────────────────────

    public async Task<List<GuardVacationPlanDto>> GetPlansByEmployeeAsync(int employeeId, int? year, CancellationToken ct)
    {
        var items = await _planRepo.GetByEmployeeAsync(employeeId, year, ct);
        return items.Select(MapPlanToDto).ToList();
    }

    public async Task<PagedResult<GuardVacationPlanDto>> GetPlansPagedAsync(int page, int pageSize, int? year, string? status, int? employeeId, DateOnly? startDate, DateOnly? endDate, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.GuardVacationPlans
            .Include(p => p.Employee).ThenInclude(e => e!.People)
            .Include(p => p.StatusType)
            .Include(p => p.DirectionApprover).ThenInclude(e => e!.People)
            .AsQueryable();

        if (year.HasValue)       q = q.Where(p => p.VacationYear == year.Value);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(p => p.StatusType!.Name == status);
        if (employeeId.HasValue) q = q.Where(p => p.EmployeeId == employeeId.Value);
        if (startDate.HasValue)  q = q.Where(p => p.PlannedStartDate >= startDate.Value);
        if (endDate.HasValue)    q = q.Where(p => p.PlannedEndDate   <= endDate.Value);

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderByDescending(p => p.VacationYear)
            .ThenBy(p => p.PlannedStartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<GuardVacationPlanDto>
        {
            Items = items.Select(MapPlanToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GuardVacationPlanDto?> GetPlanByIdAsync(int planId, CancellationToken ct)
    {
        var item = await _db.GuardVacationPlans
            .Include(p => p.Employee).ThenInclude(e => e!.People)
            .Include(p => p.StatusType)
            .Include(p => p.DirectionApprover).ThenInclude(e => e!.People)
            .Include(p => p.SubmittedByEmployee).ThenInclude(e => e!.People)
            .FirstOrDefaultAsync(p => p.GuardVacationPlanId == planId, ct);

        return item is null ? null : MapPlanToDto(item);
    }

    public async Task<GuardVacationPlanDto> CreatePlanAsync(CreateGuardVacationPlanDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear planes de vacaciones.");

        var plannedStatusId = await GetRefTypeIdAsync("GUARD_VACATION_PLAN_STATUS", "PLANNED", ct);

        var entity = new GuardVacationPlan
        {
            EmployeeId = dto.EmployeeId,
            VacationYear = dto.VacationYear,
            PlannedStartDate = dto.PlannedStartDate,
            PlannedEndDate = dto.PlannedEndDate,
            StatusTypeId = plannedStatusId,
            Notes = dto.Notes,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _planRepo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return await GetPlanByIdAsync(entity.GuardVacationPlanId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el plan de vacaciones creado.");
    }

    public async Task<GuardVacationPlanDto> UpdatePlanAsync(int planId, UpdateGuardVacationPlanDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardVacationPlans
            .FirstOrDefaultAsync(p => p.GuardVacationPlanId == planId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones {planId} no encontrado.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede actualizar planes de vacaciones.");

        entity.PlannedStartDate = dto.PlannedStartDate;
        entity.PlannedEndDate = dto.PlannedEndDate;
        entity.Notes = dto.Notes;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetPlanByIdAsync(planId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el plan de vacaciones actualizado.");
    }

    public async Task<GuardVacationPlanDto> SubmitPlanToDirectionAsync(int planId, SubmitToDirectionDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardVacationPlans
            .Include(p => p.StatusType)
            .FirstOrDefaultAsync(p => p.GuardVacationPlanId == planId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones {planId} no encontrado.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede enviar planes a dirección.");

        if (entity.StatusType?.Name != "PLANNED")
            throw new InvalidOperationException($"Solo se pueden enviar a dirección planes en estado PLANNED. Estado actual: {entity.StatusType?.Name}");

        var pendingStatusId = await GetRefTypeIdAsync("GUARD_VACATION_PLAN_STATUS", "PENDING_DIRECTION_APPROVAL", ct);

        entity.StatusTypeId = pendingStatusId;
        entity.SubmittedToDirectionBy = userId;
        entity.SubmittedToDirectionAt = DateTime.UtcNow;
        if (dto.Notes is not null) entity.Notes = dto.Notes;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetPlanByIdAsync(planId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el plan enviado a dirección.");
    }

    public async Task<GuardVacationPlanDto> ApprovePlanAsync(int planId, ApproveGuardVacationPlanDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardVacationPlans
            .Include(p => p.StatusType)
            .FirstOrDefaultAsync(p => p.GuardVacationPlanId == planId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones {planId} no encontrado.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede aprobar planes de vacaciones.");

        if (entity.EmployeeId == userId)
            throw new InvalidOperationException("No puede aprobar su propia solicitud de vacaciones.");

        if (entity.StatusType?.Name != "PENDING_DIRECTION_APPROVAL")
            throw new InvalidOperationException($"Solo se pueden aprobar planes en estado PENDING_DIRECTION_APPROVAL. Estado actual: {entity.StatusType?.Name}");

        var approvedStatusId = await GetRefTypeIdAsync("GUARD_VACATION_PLAN_STATUS", "APPROVED", ct);

        entity.StatusTypeId = approvedStatusId;
        entity.DirectionApprovedBy = userId;
        entity.DirectionApprovedAt = DateTime.UtcNow;
        if (dto.Notes is not null) entity.Notes = dto.Notes;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        // Sincronizar bloque de disponibilidad para que el generador de turnos
        // excluya automáticamente al guardia durante sus vacaciones aprobadas.
        await SyncVacationBlockAsync(entity, ct);

        return await GetPlanByIdAsync(planId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el plan aprobado.");
    }

    public async Task<GuardVacationPlanDto> RejectPlanAsync(int planId, RejectGuardVacationPlanDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardVacationPlans
            .FirstOrDefaultAsync(p => p.GuardVacationPlanId == planId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones {planId} no encontrado.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede rechazar planes de vacaciones.");

        if (entity.EmployeeId == userId)
            throw new InvalidOperationException("No puede rechazar su propia solicitud de vacaciones.");

        var rejectedStatusId = await GetRefTypeIdAsync("GUARD_VACATION_PLAN_STATUS", "REJECTED", ct);

        entity.StatusTypeId = rejectedStatusId;
        entity.Notes = dto.Reason;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetPlanByIdAsync(planId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el plan rechazado.");
    }

    // ─── Solicitudes de vacaciones ────────────────────────────────────────────

    public async Task<List<GuardVacationRequestDto>> GetRequestsByEmployeeAsync(int employeeId, CancellationToken ct)
    {
        var items = await _requestRepo.GetByEmployeeAsync(employeeId, ct);
        return items.Select(MapRequestToDto).ToList();
    }

    public async Task<PagedResult<GuardVacationRequestDto>> GetRequestsPagedAsync(int page, int pageSize, string? status, int? employeeId, DateOnly? startDate, DateOnly? endDate, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.GuardVacationRequests
            .Include(r => r.Employee).ThenInclude(e => e!.People)
            .Include(r => r.RequestType)
            .Include(r => r.StatusType)
            .Include(r => r.Requester).ThenInclude(e => e!.People)
            .Include(r => r.DirectionApprover).ThenInclude(e => e!.People)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(r => r.StatusType!.Name == status);
        if (employeeId.HasValue) q = q.Where(r => r.EmployeeId == employeeId.Value);
        if (startDate.HasValue)  q = q.Where(r => r.OriginalStartDate >= startDate.Value);
        if (endDate.HasValue)    q = q.Where(r => r.OriginalEndDate   <= endDate.Value);

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderByDescending(r => r.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<GuardVacationRequestDto>
        {
            Items = items.Select(MapRequestToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GuardVacationRequestDto?> GetRequestByIdAsync(int requestId, CancellationToken ct)
    {
        var item = await _db.GuardVacationRequests
            .Include(r => r.Employee).ThenInclude(e => e!.People)
            .Include(r => r.RequestType)
            .Include(r => r.StatusType)
            .Include(r => r.Requester).ThenInclude(e => e!.People)
            .Include(r => r.DirectionApprover).ThenInclude(e => e!.People)
            .Include(r => r.SubmittedByEmployee).ThenInclude(e => e!.People)
            .FirstOrDefaultAsync(r => r.GuardVacationRequestId == requestId, ct);

        return item is null ? null : MapRequestToDto(item);
    }

    public async Task<GuardVacationRequestDto> CreateChangeDatesRequestAsync(CreateChangeDatesRequestDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear solicitudes.");

        if (dto.GuardVacationPlanId.HasValue)
        {
            var plan = await _db.GuardVacationPlans
                .Include(p => p.StatusType)
                .FirstOrDefaultAsync(p => p.GuardVacationPlanId == dto.GuardVacationPlanId.Value, ct)
                ?? throw new KeyNotFoundException($"Plan de vacaciones {dto.GuardVacationPlanId} no encontrado.");

            if (plan.StatusType?.Name != "APPROVED")
                throw new InvalidOperationException("Solo se pueden solicitar cambios de fechas sobre planes de vacaciones APROBADOS.");
        }

        var requestTypeId = await GetRefTypeIdAsync("GUARD_VACATION_REQUEST_TYPE", "CHANGE_DATES", ct);
        var requestedStatusId = await GetRefTypeIdAsync("GUARD_VACATION_REQUEST_STATUS", "REQUESTED", ct);

        var entity = new GuardVacationRequest
        {
            EmployeeId = dto.EmployeeId,
            GuardVacationPlanId = dto.GuardVacationPlanId,
            RequestTypeId = requestTypeId,
            OriginalStartDate = dto.OriginalStartDate,
            OriginalEndDate = dto.OriginalEndDate,
            RequestedStartDate = dto.RequestedStartDate,
            RequestedEndDate = dto.RequestedEndDate,
            SourceYear = dto.SourceYear,
            Reason = dto.Reason,
            StatusTypeId = requestedStatusId,
            RequestedBy = userId,
            RequestedAt = DateTime.UtcNow,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _requestRepo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return await GetRequestByIdAsync(entity.GuardVacationRequestId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la solicitud creada.");
    }

    public async Task<GuardVacationRequestDto> CreateAccumulateRequestAsync(CreateAccumulateRequestDto dto, CancellationToken ct)
    {
        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede crear solicitudes.");

        if (dto.GuardVacationPlanId.HasValue)
        {
            var plan = await _db.GuardVacationPlans
                .Include(p => p.StatusType)
                .FirstOrDefaultAsync(p => p.GuardVacationPlanId == dto.GuardVacationPlanId.Value, ct)
                ?? throw new KeyNotFoundException($"Plan de vacaciones {dto.GuardVacationPlanId} no encontrado.");

            if (plan.StatusType?.Name != "APPROVED")
                throw new InvalidOperationException("Solo se pueden solicitar acumulación sobre planes de vacaciones APROBADOS.");
        }

        var requestTypeId = await GetRefTypeIdAsync("GUARD_VACATION_REQUEST_TYPE", "ACCUMULATE_NEXT_YEAR", ct);
        var requestedStatusId = await GetRefTypeIdAsync("GUARD_VACATION_REQUEST_STATUS", "REQUESTED", ct);

        var entity = new GuardVacationRequest
        {
            EmployeeId = dto.EmployeeId,
            GuardVacationPlanId = dto.GuardVacationPlanId,
            RequestTypeId = requestTypeId,
            OriginalStartDate = dto.OriginalStartDate,
            OriginalEndDate = dto.OriginalEndDate,
            SourceYear = dto.SourceYear,
            TargetYear = dto.TargetYear,
            Reason = dto.Reason,
            StatusTypeId = requestedStatusId,
            RequestedBy = userId,
            RequestedAt = DateTime.UtcNow,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _requestRepo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return await GetRequestByIdAsync(entity.GuardVacationRequestId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la solicitud creada.");
    }

    public async Task<GuardVacationRequestDto> SubmitRequestToDirectionAsync(int requestId, SubmitToDirectionDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardVacationRequests
            .Include(r => r.StatusType)
            .FirstOrDefaultAsync(r => r.GuardVacationRequestId == requestId, ct)
            ?? throw new KeyNotFoundException($"Solicitud de vacaciones {requestId} no encontrada.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede enviar solicitudes a dirección.");

        if (entity.StatusType?.Name != "REQUESTED")
            throw new InvalidOperationException($"Solo se pueden enviar a dirección solicitudes en estado REQUESTED. Estado actual: {entity.StatusType?.Name}");

        var pendingStatusId = await GetRefTypeIdAsync("GUARD_VACATION_REQUEST_STATUS", "PENDING_DIRECTION_APPROVAL", ct);

        entity.StatusTypeId = pendingStatusId;
        entity.SubmittedToDirectionBy = userId;
        entity.SubmittedToDirectionAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetRequestByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la solicitud enviada a dirección.");
    }

    public async Task<GuardVacationRequestDto> ApproveRequestAsync(int requestId, ApproveGuardVacationRequestDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardVacationRequests
            .Include(r => r.StatusType)
            .FirstOrDefaultAsync(r => r.GuardVacationRequestId == requestId, ct)
            ?? throw new KeyNotFoundException($"Solicitud de vacaciones {requestId} no encontrada.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede aprobar solicitudes.");

        if (entity.StatusType?.Name != "PENDING_DIRECTION_APPROVAL")
            throw new InvalidOperationException($"Solo se pueden aprobar solicitudes en estado PENDING_DIRECTION_APPROVAL. Estado actual: {entity.StatusType?.Name}");

        var approvedStatusId = await GetRefTypeIdAsync("GUARD_VACATION_REQUEST_STATUS", "APPROVED", ct);

        entity.StatusTypeId = approvedStatusId;
        entity.DirectionApprovedBy = userId;
        entity.DirectionApprovedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetRequestByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la solicitud aprobada.");
    }

    public async Task<GuardVacationRequestDto> RejectRequestAsync(int requestId, RejectGuardVacationRequestDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardVacationRequests
            .FirstOrDefaultAsync(r => r.GuardVacationRequestId == requestId, ct)
            ?? throw new KeyNotFoundException($"Solicitud de vacaciones {requestId} no encontrada.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede rechazar solicitudes.");

        var rejectedStatusId = await GetRefTypeIdAsync("GUARD_VACATION_REQUEST_STATUS", "REJECTED", ct);

        entity.StatusTypeId = rejectedStatusId;
        entity.RejectedBy = userId;
        entity.RejectedAt = DateTime.UtcNow;
        entity.RejectionReason = dto.Reason;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await GetRequestByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la solicitud rechazada.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task SyncVacationBlockAsync(GuardVacationPlan plan, CancellationToken ct)
    {
        var sourceTable = "tbl_GuardVacationPlans";
        var sourceId = plan.GuardVacationPlanId.ToString();

        // Cancelar bloque previo del mismo origen si existe
        var existing = await _db.EmployeeAvailabilityBlocks
            .Where(b => b.SourceTable == sourceTable && b.SourceId == sourceId)
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            var cancelledStatusId = await _db.Set<RefTypes>()
                .Where(r => r.Category == "GUARD_BLOCK_STATUS" && r.Name == "CANCELLED")
                .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

            foreach (var b in existing)
                b.StatusTypeId = cancelledStatusId;
        }

        var vacSourceTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_SOURCE" && r.Name == "VACATION")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var activeTypeId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_STATUS" && r.Name == "ACTIVE")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var block = new EmployeeAvailabilityBlock
        {
            EmployeeId = plan.EmployeeId,
            SourceTypeId = vacSourceTypeId,
            SourceTable = sourceTable,
            SourceId = sourceId,
            StartDateTime = plan.PlannedStartDate.ToDateTime(TimeOnly.MinValue),
            EndDateTime = plan.PlannedEndDate.ToDateTime(TimeOnly.MaxValue),
            StatusTypeId = activeTypeId,
            Reason = $"Vacaciones plan {plan.GuardVacationPlanId} año {plan.VacationYear}"
        };

        await _db.EmployeeAvailabilityBlocks.AddAsync(block, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> GetRefTypeIdAsync(string category, string name, CancellationToken ct)
    {
        var id = await _db.Set<RefTypes>()
            .Where(r => r.Category == category && r.Name == name)
            .Select(r => r.TypeId)
            .FirstOrDefaultAsync(ct);

        if (id == 0) throw new InvalidOperationException($"RefType no encontrado: {category}/{name}");
        return id;
    }

    private static GuardVacationPlanDto MapPlanToDto(GuardVacationPlan p) =>
        new(
            p.GuardVacationPlanId,
            p.EmployeeId,
            p.Employee is null ? string.Empty : $"{p.Employee.People?.FirstName} {p.Employee.People?.LastName}".Trim(),
            p.Employee?.People?.IdCard,
            p.VacationYear,
            p.PlannedStartDate,
            p.PlannedEndDate,
            p.StatusTypeId,
            p.StatusType?.Name ?? string.Empty,
            p.DirectionApprovedBy,
            p.DirectionApprover is null ? null : $"{p.DirectionApprover.People?.FirstName} {p.DirectionApprover.People?.LastName}".Trim(),
            p.DirectionApprovedAt,
            p.SubmittedToDirectionBy,
            p.SubmittedByEmployee is null ? null : $"{p.SubmittedByEmployee.People?.FirstName} {p.SubmittedByEmployee.People?.LastName}".Trim(),
            p.SubmittedToDirectionAt,
            p.Notes
        );

    private static GuardVacationRequestDto MapRequestToDto(GuardVacationRequest r) =>
        new(
            r.GuardVacationRequestId,
            r.EmployeeId,
            r.Employee is null ? string.Empty : $"{r.Employee.People?.FirstName} {r.Employee.People?.LastName}".Trim(),
            r.Employee?.People?.IdCard,
            r.GuardVacationPlanId,
            r.VacationId,
            r.RequestType?.Name ?? string.Empty,
            r.OriginalStartDate,
            r.OriginalEndDate,
            r.RequestedStartDate,
            r.RequestedEndDate,
            r.SourceYear,
            r.TargetYear,
            r.Reason,
            r.StatusType?.Name ?? string.Empty,
            r.RequestedBy,
            r.Requester is null ? null : $"{r.Requester.People?.FirstName} {r.Requester.People?.LastName}".Trim(),
            r.RequestedAt,
            r.DirectionApprovedBy,
            r.DirectionApprover is null ? null : $"{r.DirectionApprover.People?.FirstName} {r.DirectionApprover.People?.LastName}".Trim(),
            r.DirectionApprovedAt,
            r.SubmittedToDirectionBy,
            r.SubmittedByEmployee is null ? null : $"{r.SubmittedByEmployee.People?.FirstName} {r.SubmittedByEmployee.People?.LastName}".Trim(),
            r.SubmittedToDirectionAt,
            r.RejectionReason,
            r.RejectedAt
        );
}

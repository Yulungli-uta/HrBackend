using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Extensions;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Application.Interfaces.Services;
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
    private readonly IHrBalanceService _balanceService;
    private readonly IVacationsService _vacationsService;
    private readonly GuardVacationFlowContext _guardFlow;

    private readonly IAppClock _clock;

    public GuardVacationService(
        IGuardVacationPlanRepository planRepo,
        IGuardVacationRequestRepository requestRepo,
        AppDbContext db,
        ICurrentUserService currentUser,
        IHrBalanceService balanceService,
        IVacationsService vacationsService,
        GuardVacationFlowContext guardFlow,
        IAppClock clock)
    {
        _clock = clock;
        _planRepo = planRepo;
        _requestRepo = requestRepo;
        _db = db;
        _currentUser = currentUser;
        _balanceService = balanceService;
        _vacationsService = vacationsService;
        _guardFlow = guardFlow;
    }

    // ─── Planes de vacaciones ─────────────────────────────────────────────────

    public async Task<List<GuardVacationPlanDto>> GetPlansByEmployeeAsync(int employeeId, int? year, CancellationToken ct)
    {
        var items = await _planRepo.GetByEmployeeAsync(employeeId, year, ct);
        return items.Select(MapPlanToDto).ToList();
    }

    public async Task<PagedResult<GuardVacationPlanDto>> GetPlansPagedAsync(int page, int pageSize, int? year, string? status, int? employeeId, DateOnly? startDate, DateOnly? endDate, string? search, CancellationToken ct)
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
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(p =>
                (p.Employee!.People!.LastName + " " + p.Employee.People.FirstName).ToLower().Contains(term) ||
                (p.Employee.People.IdCard != null && p.Employee.People.IdCard.ToLower().Contains(term)));
        }

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderByDescending(p => p.VacationYear)
            .ThenBy(p => p.PlannedStartDate)
            .ThenBy(p => p.Employee!.People!.LastName)
            .ThenBy(p => p.Employee!.People!.FirstName)
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

        await ValidateWithinVacationBalanceAsync(dto.EmployeeId, dto.PlannedStartDate, dto.PlannedEndDate, ct);

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
            CreatedAt = _clock.Now
        };

        await _planRepo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        return await GetPlanByIdAsync(entity.GuardVacationPlanId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el plan de vacaciones creado.");
    }

    public async Task<GuardVacationPlanDto> UpdatePlanAsync(int planId, UpdateGuardVacationPlanDto dto, CancellationToken ct)
    {
        var entity = await _db.GuardVacationPlans
            .Include(p => p.StatusType)
            .FirstOrDefaultAsync(p => p.GuardVacationPlanId == planId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones {planId} no encontrado.");

        var userId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede actualizar planes de vacaciones.");

        if (entity.StatusType?.Name != "PLANNED")
            throw new InvalidOperationException($"Solo se pueden editar planes en estado PLANNED. Estado actual: {entity.StatusType?.Name}");

        await ValidateWithinVacationBalanceAsync(entity.EmployeeId, dto.PlannedStartDate, dto.PlannedEndDate, ct);

        entity.PlannedStartDate = dto.PlannedStartDate;
        entity.PlannedEndDate = dto.PlannedEndDate;
        entity.Notes = dto.Notes;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = _clock.Now;

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
        entity.SubmittedToDirectionAt = _clock.Now;
        if (dto.Notes is not null) entity.Notes = dto.Notes;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = _clock.Now;

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

        // La vacación aprobada se registra en tbl_Vacations (reserva y consume saldo) para que el
        // cruce de asistencia la reconozca; antes solo se creaba el bloqueo de disponibilidad y los
        // días del plan salían como ausencia. Si no hay saldo, la aprobación se rechaza completa.
        var vacationId = await CreateApprovedVacationAsync(entity, userId, ct);

        entity.StatusTypeId = approvedStatusId;
        entity.DirectionApprovedBy = userId;
        entity.DirectionApprovedAt = _clock.Now;
        if (dto.Notes is not null) entity.Notes = dto.Notes;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = _clock.Now;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            await CancelVacationAsync(vacationId, ct);
            throw;
        }

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

        // Si el plan ya estaba aprobado, su vacación se cancela y se libera el saldo reservado.
        await CancelLinkedVacationAsync(entity, ct);

        entity.StatusTypeId = rejectedStatusId;
        entity.Notes = dto.Reason;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(ct);

        // Si el plan ya estaba APPROVED (SyncVacationBlockAsync le creo un bloqueo ACTIVO),
        // rechazarlo ahora no debe dejar ese bloqueo colgado -- si no, el generador de turnos
        // sigue excluyendo al guardia aunque el plan ya este rechazado (hallazgo real
        // 2026-10-05, empleado 5322, plan 82).
        await CancelVacationBlocksAsync(planId, ct);

        return await GetPlanByIdAsync(planId, ct)
            ?? throw new InvalidOperationException("Error al recuperar el plan rechazado.");
    }

    // ─── Solicitudes de vacaciones ────────────────────────────────────────────

    public async Task<List<GuardVacationRequestDto>> GetRequestsByEmployeeAsync(int employeeId, CancellationToken ct)
    {
        var items = await _requestRepo.GetByEmployeeAsync(employeeId, ct);
        return items.Select(MapRequestToDto).ToList();
    }

    public async Task<PagedResult<GuardVacationRequestDto>> GetRequestsPagedAsync(int page, int pageSize, string? status, int? employeeId, DateOnly? startDate, DateOnly? endDate, string? search, CancellationToken ct)
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
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(r =>
                (r.Employee!.People!.LastName + " " + r.Employee.People.FirstName).ToLower().Contains(term) ||
                (r.Employee.People.IdCard != null && r.Employee.People.IdCard.ToLower().Contains(term)));
        }

        var total = await q.LongCountAsync(ct);
        var items = await q
            .OrderBy(r => r.OriginalStartDate)
            .ThenBy(r => r.Employee!.People!.LastName)
            .ThenBy(r => r.Employee!.People!.FirstName)
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
            RequestedAt = _clock.Now,
            CreatedBy = userId,
            CreatedAt = _clock.Now
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
            RequestedAt = _clock.Now,
            CreatedBy = userId,
            CreatedAt = _clock.Now
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
        entity.SubmittedToDirectionAt = _clock.Now;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = _clock.Now;

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
        var appliedStatusId = await GetRefTypeIdAsync("GUARD_VACATION_REQUEST_STATUS", "APPLIED", ct);

        entity.StatusTypeId = approvedStatusId;
        entity.DirectionApprovedBy = userId;
        entity.DirectionApprovedAt = _clock.Now;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = _clock.Now;

        // Aprobar una solicitud de cambio de fechas o de acumulación ya modifica de verdad el plan,
        // el bloqueo de disponibilidad y la vacación; antes solo cambiaba el estado de la solicitud.
        if (entity.GuardVacationPlanId.HasValue)
        {
            var requestType = await _db.GuardVacationRequests
                .Where(r => r.GuardVacationRequestId == requestId)
                .Select(r => r.RequestType!.Name)
                .FirstAsync(ct);

            var vacationId = requestType switch
            {
                "CHANGE_DATES" => await ApplyChangeDatesAsync(entity, ct),
                "ACCUMULATE_NEXT_YEAR" => await ApplyAccumulateAsync(entity, ct),
                _ => (int?)null
            };

            if (vacationId.HasValue) entity.VacationId = vacationId;
            if (requestType is "CHANGE_DATES" or "ACCUMULATE_NEXT_YEAR") entity.StatusTypeId = appliedStatusId;
        }

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
        entity.RejectedAt = _clock.Now;
        entity.RejectionReason = dto.Reason;
        entity.UpdatedBy = userId;
        entity.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(ct);

        return await GetRequestByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("Error al recuperar la solicitud rechazada.");
    }

    // ─── Aplicación de solicitudes aprobadas ─────────────────────────────────

    /// <summary>
    /// Cambio de fechas aprobado: valida que no choque con turnos ni con otros planes del guardia y
    /// mueve el plan, su bloqueo de disponibilidad y la vacación (que recalcula la reserva de saldo).
    /// </summary>
    private async Task<int?> ApplyChangeDatesAsync(GuardVacationRequest request, CancellationToken ct)
    {
        var plan = await LoadApprovedPlanForRequestAsync(request, ct);

        if (request.RequestedStartDate is not { } newStart || request.RequestedEndDate is not { } newEnd)
            throw new BusinessRuleException("La solicitud de cambio de fechas no indica las fechas nuevas.");
        if (newEnd < newStart)
            throw new BusinessRuleException("La fecha final solicitada no puede ser anterior a la inicial.");

        var shifts = await _db.GuardShiftPlannings
            .Where(g => g.EmployeeId == plan.EmployeeId && g.IsActiveForAssignment
                        && g.WorkDate >= newStart && g.WorkDate <= newEnd)
            .CountAsync(ct);
        if (shifts > 0)
            throw new BusinessRuleException(
                $"El guardia tiene {shifts} turno(s) programado(s) entre {newStart:dd/MM/yyyy} y {newEnd:dd/MM/yyyy}. " +
                "Reasígnelos o cancélelos antes de aprobar el cambio de fechas.");

        var overlapsPlan = await _db.GuardVacationPlans
            .AnyAsync(p => p.EmployeeId == plan.EmployeeId
                           && p.GuardVacationPlanId != plan.GuardVacationPlanId
                           && p.StatusType!.Name == "APPROVED"
                           && p.PlannedStartDate <= newEnd && p.PlannedEndDate >= newStart, ct);
        if (overlapsPlan)
            throw new BusinessRuleException("Las fechas solicitadas se cruzan con otro plan de vacaciones aprobado del guardia.");

        var oldStart = plan.PlannedStartDate;
        var oldEnd = plan.PlannedEndDate;

        // 1) vacación (transacción propia): recalcula la reserva; si no hay saldo para las fechas nuevas falla aquí
        var vacation = await FindLinkedVacationAsync(plan.EmployeeId, oldStart, oldEnd, ct);
        if (vacation is not null && vacation.Status == "Planned")
        {
            var days = newEnd.DayNumber - newStart.DayNumber + 1;
            using var flow = _guardFlow.Begin();
            await _vacationsService.UpdateBalanceAffectAsync(vacation.VacationId, new Vacations
            {
                VacationId = vacation.VacationId,
                EmployeeId = vacation.EmployeeId,
                StartDate = newStart,
                EndDate = newEnd,
                DaysGranted = days,
                DaysTaken = days,
                Status = vacation.Status,
                ApprovedBy = vacation.ApprovedBy,
                ApprovedAt = vacation.ApprovedAt,
                CreatedBy = vacation.CreatedBy,
                CreatedAt = vacation.CreatedAt
            }, ct);
        }

        // 2) plan + bloqueo; si falla, se devuelve la vacación a las fechas originales
        try
        {
            plan.PlannedStartDate = newStart;
            plan.PlannedEndDate = newEnd;
            plan.Notes = AppendNote(plan.Notes, $"Fechas cambiadas {oldStart:dd/MM/yyyy}-{oldEnd:dd/MM/yyyy} a {newStart:dd/MM/yyyy}-{newEnd:dd/MM/yyyy} (solicitud {request.GuardVacationRequestId})");
            plan.UpdatedAt = _clock.Now;
            await _db.SaveChangesAsync(ct);
            await SyncVacationBlockAsync(plan, ct);
        }
        catch
        {
            if (vacation is not null && vacation.Status == "Planned")
            {
                var days = oldEnd.DayNumber - oldStart.DayNumber + 1;
                using var flow = _guardFlow.Begin();
                await _vacationsService.UpdateBalanceAffectAsync(vacation.VacationId, new Vacations
                {
                    VacationId = vacation.VacationId, EmployeeId = vacation.EmployeeId, StartDate = oldStart, EndDate = oldEnd,
                    DaysGranted = days, DaysTaken = days, Status = vacation.Status, ApprovedBy = vacation.ApprovedBy,
                    ApprovedAt = vacation.ApprovedAt, CreatedBy = vacation.CreatedBy, CreatedAt = vacation.CreatedAt
                }, ct);
            }
            throw;
        }

        return vacation?.VacationId;
    }

    /// <summary>
    /// Acumulación aprobada: se cancela la vacación (libera el saldo reservado, que queda disponible para
    /// el año siguiente dentro del tope de acumulación) y el bloqueo, y el plan pasa a ACCUMULATED.
    /// El supervisor registra el plan del año siguiente cuando corresponda.
    /// </summary>
    private async Task<int?> ApplyAccumulateAsync(GuardVacationRequest request, CancellationToken ct)
    {
        var plan = await LoadApprovedPlanForRequestAsync(request, ct);

        var vacation = await FindLinkedVacationAsync(plan.EmployeeId, plan.PlannedStartDate, plan.PlannedEndDate, ct);
        if (vacation is not null)
            await CancelVacationAsync(vacation.VacationId, ct);

        plan.StatusTypeId = await GetRefTypeIdAsync("GUARD_VACATION_PLAN_STATUS", "ACCUMULATED", ct);
        plan.Notes = AppendNote(plan.Notes, $"Acumulado al año {request.TargetYear?.ToString() ?? "siguiente"} (solicitud {request.GuardVacationRequestId})");
        plan.UpdatedAt = _clock.Now;
        await _db.SaveChangesAsync(ct);
        await CancelVacationBlocksAsync(plan.GuardVacationPlanId, ct);

        return vacation?.VacationId;
    }

    private async Task<GuardVacationPlan> LoadApprovedPlanForRequestAsync(GuardVacationRequest request, CancellationToken ct)
    {
        var plan = await _db.GuardVacationPlans
            .Include(p => p.StatusType)
            .FirstOrDefaultAsync(p => p.GuardVacationPlanId == request.GuardVacationPlanId, ct)
            ?? throw new KeyNotFoundException($"Plan de vacaciones {request.GuardVacationPlanId} no encontrado.");

        if (plan.StatusType?.Name != "APPROVED")
            throw new BusinessRuleException($"El plan de vacaciones ya no está aprobado (estado actual: {plan.StatusType?.Name}).");

        return plan;
    }

    private Task<Vacations?> FindLinkedVacationAsync(int employeeId, DateOnly start, DateOnly end, CancellationToken ct) =>
        _db.Vacations.AsNoTracking().FirstOrDefaultAsync(v => v.EmployeeId == employeeId
                                                              && v.StartDate == start && v.EndDate == end
                                                              && v.Status != "Canceled", ct);

    private static string AppendNote(string? current, string note) =>
        string.IsNullOrWhiteSpace(current) ? note : $"{current} | {note}";

    // ─── Helpers ──────────────────────────────────────────────────────────────

    // Cancela cualquier bloqueo de disponibilidad que un plan de vacaciones haya generado
    // (via SyncVacationBlockAsync al aprobarlo). Reutilizado al reemplazar el bloqueo por uno
    // nuevo y al rechazar un plan que ya estaba aprobado.
    private async Task CancelVacationBlocksAsync(int planId, CancellationToken ct)
    {
        var sourceTable = "tbl_GuardVacationPlans";
        var sourceId = planId.ToString();

        var cancelledStatusId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_STATUS" && r.Name == "CANCELLED")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var activeStatusId = await _db.Set<RefTypes>()
            .Where(r => r.Category == "GUARD_BLOCK_STATUS" && r.Name == "ACTIVE")
            .Select(r => r.TypeId).FirstOrDefaultAsync(ct);

        var existing = await _db.EmployeeAvailabilityBlocks
            .Where(b => b.SourceTable == sourceTable && b.SourceId == sourceId && b.StatusTypeId == activeStatusId)
            .ToListAsync(ct);

        if (existing.Count == 0) return;

        foreach (var b in existing)
            b.StatusTypeId = cancelledStatusId;

        await _db.SaveChangesAsync(ct);
    }

    private async Task SyncVacationBlockAsync(GuardVacationPlan plan, CancellationToken ct)
    {
        var sourceTable = "tbl_GuardVacationPlans";
        var sourceId = plan.GuardVacationPlanId.ToString();

        await CancelVacationBlocksAsync(plan.GuardVacationPlanId, ct);

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

    /// <summary>
    /// Registra la vacación del plan aprobado en HR.tbl_Vacations. Queda en Planned con el saldo reservado
    /// (no consumido): así los días se pueden devolver si el plan se rechaza, cambia de fechas o se acumula;
    /// una reserva ya consumida (estado Approved) no admite devolución en el libro de saldos.
    /// La aprobación de la dirección queda en ApprovedBy / ApprovedAt.
    /// </summary>
    private async Task<int> CreateApprovedVacationAsync(GuardVacationPlan plan, int approvedBy, CancellationToken ct)
    {
        var days = plan.PlannedEndDate.DayNumber - plan.PlannedStartDate.DayNumber + 1;

        using var flow = _guardFlow.Begin();
        var created = await _vacationsService.CreateWithBalanceCheckAsync(new Vacations
        {
            EmployeeId = plan.EmployeeId,
            StartDate = plan.PlannedStartDate,
            EndDate = plan.PlannedEndDate,
            DaysGranted = days,
            DaysTaken = days,
            Status = "Planned",
            ApprovedBy = approvedBy,
            ApprovedAt = _clock.Now,
            CreatedBy = approvedBy
        }, ct);

        return created.VacationId;
    }

    /// <summary>Cancela la vacación activa del empleado con las mismas fechas que el plan (si existe).</summary>
    private async Task CancelLinkedVacationAsync(GuardVacationPlan plan, CancellationToken ct)
    {
        var vacationId = await _db.Vacations
            .AsNoTracking()
            .Where(v => v.EmployeeId == plan.EmployeeId
                        && v.StartDate == plan.PlannedStartDate
                        && v.EndDate == plan.PlannedEndDate
                        && v.Status != "Canceled")
            .Select(v => (int?)v.VacationId)
            .FirstOrDefaultAsync(ct);

        if (vacationId.HasValue)
            await CancelVacationAsync(vacationId.Value, ct);
    }

    private async Task CancelVacationAsync(int vacationId, CancellationToken ct)
    {
        using var flow = _guardFlow.Begin();
        var current = await _db.Vacations.AsNoTracking().FirstOrDefaultAsync(v => v.VacationId == vacationId, ct);
        if (current is not null && current.Status != "Canceled")
            await ChangeVacationStatusAsync(current, "Canceled", ct);
    }

    // UpdateBalanceAffectAsync reemplaza la fila completa con el payload, por eso se manda una copia
    // íntegra de la vacación actual con solo el estado cambiado.
    private Task ChangeVacationStatusAsync(Vacations current, string newStatus, CancellationToken ct) =>
        _vacationsService.UpdateBalanceAffectAsync(current.VacationId, new Vacations
        {
            VacationId = current.VacationId,
            EmployeeId = current.EmployeeId,
            StartDate = current.StartDate,
            EndDate = current.EndDate,
            DaysGranted = current.DaysGranted,
            DaysTaken = current.DaysTaken,
            Status = newStatus,
            ApprovedBy = current.ApprovedBy,
            ApprovedAt = current.ApprovedAt,
            CreatedBy = current.CreatedBy,
            CreatedAt = current.CreatedAt
        }, ct);

    private async Task<int> GetRefTypeIdAsync(string category, string name, CancellationToken ct)
    {
        var id = await _db.Set<RefTypes>()
            .Where(r => r.Category == category && r.Name == name)
            .Select(r => r.TypeId)
            .FirstOrDefaultAsync(ct);

        if (id == 0) throw new InvalidOperationException($"RefType no encontrado: {category}/{name}");
        return id;
    }

    // Reutiliza el saldo real de vacaciones del empleado (el mismo que usa el módulo general
    // de Vacaciones vía IHrBalanceService -- ya resuelve régimen LOSEP/LOES vs Código de
    // Trabajo, antigüedad y acumulación) en vez de un tope fijo propio de Guardias
    // (pedido del usuario 2026-10-05, QA obs. 40).
    private async Task ValidateWithinVacationBalanceAsync(int employeeId, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var requestedDays = endDate.DayNumber - startDate.DayNumber + 1;
        if (requestedDays <= 0)
            throw new InvalidOperationException("La fecha final del plan debe ser posterior o igual a la fecha de inicio.");

        var (balance, _) = await _balanceService.GetBalancesAsync(employeeId);

        if (requestedDays > balance.VacationDays)
            throw new InvalidOperationException(
                $"El empleado solicita {requestedDays} día(s) de vacaciones, pero su saldo disponible es de {balance.VacationDays:F1} día(s).");
    }

    private static GuardVacationPlanDto MapPlanToDto(GuardVacationPlan p) =>
        new(
            p.GuardVacationPlanId,
            p.EmployeeId,
            p.Employee is null ? string.Empty : p.Employee.People.GetFullName(),
            p.Employee?.People?.IdCard,
            p.VacationYear,
            p.PlannedStartDate,
            p.PlannedEndDate,
            p.StatusTypeId,
            p.StatusType?.Name ?? string.Empty,
            p.DirectionApprovedBy,
            p.DirectionApprover is null ? null : p.DirectionApprover.People.GetFullName(),
            p.DirectionApprovedAt,
            p.SubmittedToDirectionBy,
            p.SubmittedByEmployee is null ? null : p.SubmittedByEmployee.People.GetFullName(),
            p.SubmittedToDirectionAt,
            p.Notes
        );

    private static GuardVacationRequestDto MapRequestToDto(GuardVacationRequest r) =>
        new(
            r.GuardVacationRequestId,
            r.EmployeeId,
            r.Employee is null ? string.Empty : r.Employee.People.GetFullName(),
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
            r.Requester is null ? null : r.Requester.People.GetFullName(),
            r.RequestedAt,
            r.DirectionApprovedBy,
            r.DirectionApprover is null ? null : r.DirectionApprover.People.GetFullName(),
            r.DirectionApprovedAt,
            r.SubmittedToDirectionBy,
            r.SubmittedByEmployee is null ? null : r.SubmittedByEmployee.People.GetFullName(),
            r.SubmittedToDirectionAt,
            r.RejectionReason,
            r.RejectedAt
        );
}

using WsUtaSystem.Application.DTOs.EmployeeInternalRequest;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Solicitudes internas genéricas del empleado autenticado (actualización de datos que
/// requiere revisión, documentos, información, otros trámites). Mismo patrón que
/// <see cref="ResignationRetirementService"/>: EmployeeId siempre resuelto por el caller
/// (nunca enviado por el frontend), estado con historial, RowVersion para concurrencia.
/// Flujo: PENDIENTE → (EN_REVISION) → APROBADO → COMPLETADO | RECHAZADO | DEVUELTO (→ PENDIENTE al reenviar) | ANULADO.
/// </summary>
public sealed class EmployeeInternalRequestService : IEmployeeInternalRequestService
{
    private static readonly string[] EditableStatuses =
    [
        EmployeeInternalRequestStatus.Pendiente,
        EmployeeInternalRequestStatus.Devuelto
    ];

    private static readonly string[] ReviewableStatuses =
    [
        EmployeeInternalRequestStatus.Pendiente,
        EmployeeInternalRequestStatus.EnRevision
    ];

    private static readonly string[] TerminalStatuses =
    [
        EmployeeInternalRequestStatus.Rechazado,
        EmployeeInternalRequestStatus.Anulado,
        EmployeeInternalRequestStatus.Completado
    ];

    private readonly IEmployeeInternalRequestRepository _repository;

    public EmployeeInternalRequestService(IEmployeeInternalRequestRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto> CreateAsync(int employeeId, CreateEmployeeInternalRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (employeeId <= 0)
            throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

        if (!EmployeeInternalRequestType.All.Contains(request.RequestType))
            throw new ArgumentException($"RequestType debe ser uno de: {string.Join(", ", EmployeeInternalRequestType.All)}.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new ArgumentException("El asunto de la solicitud es obligatorio.", nameof(request));

        var entity = new EmployeeInternalRequest
        {
            EmployeeId = employeeId,
            RequestType = request.RequestType,
            Subject = request.Subject,
            Description = request.Description,
            Status = EmployeeInternalRequestStatus.Pendiente
        };

        await _repository.AddAsync(entity, ct);
        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new EmployeeInternalRequestStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = null,
            NewStatus = EmployeeInternalRequestStatus.Pendiente,
            Action = "CREATED",
            CreatedAt = DateTime.Now,
            CreatedBy = employeeId
        }, ct);
        await _repository.SaveChangesAsync(ct);

        return await _repository.GetDetailByIdAsync(entity.RequestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud recién creada.");
    }

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto> UpdateAsync(int requestId, int employeeId, UpdateEmployeeInternalRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (entity.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("La solicitud no pertenece al usuario autenticado.");

        if (!EditableStatuses.Contains(entity.Status))
            throw new InvalidOperationException($"No se puede editar una solicitud en estado '{entity.Status}'.");

        EnsureRowVersionMatches(entity.RowVersion, request.RowVersion);

        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new ArgumentException("El asunto de la solicitud es obligatorio.", nameof(request));

        var previousStatus = entity.Status;
        var wasReturned = entity.Status == EmployeeInternalRequestStatus.Devuelto;

        entity.Subject = request.Subject;
        entity.Description = request.Description;
        if (wasReturned) entity.Status = EmployeeInternalRequestStatus.Pendiente;

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new EmployeeInternalRequestStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = previousStatus,
            NewStatus = entity.Status,
            Action = wasReturned ? "RESUBMITTED" : "UPDATED",
            Observation = wasReturned ? "Reenviada por el solicitante tras corrección." : null,
            CreatedAt = DateTime.Now,
            CreatedBy = employeeId
        }, ct);
        await _repository.SaveChangesAsync(ct);

        return await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud actualizada.");
    }

    /// <inheritdoc/>
    public async Task CancelOwnAsync(int requestId, int employeeId, CancelEmployeeInternalRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("El motivo de cancelación es obligatorio.", nameof(request));

        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (entity.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("La solicitud no pertenece al usuario autenticado.");

        if (TerminalStatuses.Contains(entity.Status) || entity.Status == EmployeeInternalRequestStatus.Aprobado)
            throw new InvalidOperationException($"No se puede cancelar una solicitud en estado '{entity.Status}'.");

        EnsureRowVersionMatches(entity.RowVersion, request.RowVersion);

        var previousStatus = entity.Status;
        entity.Status = EmployeeInternalRequestStatus.Anulado;
        entity.CancelledAt = DateTime.Now;
        entity.CancelledBy = employeeId;

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new EmployeeInternalRequestStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = previousStatus,
            NewStatus = EmployeeInternalRequestStatus.Anulado,
            Action = "CANCELLED",
            Observation = request.Reason,
            CreatedAt = DateTime.Now,
            CreatedBy = employeeId
        }, ct);
        await _repository.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<PagedEmployeeInternalRequestResult> GetMyRequestsAsync(int employeeId, EmployeeInternalRequestQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        var ownFilter = filter with { EmployeeId = employeeId, AllowedDepartmentIds = null };
        return await _repository.GetPagedAsync(ownFilter, ct);
    }

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto> GetMyRequestDetailAsync(int requestId, int employeeId, CancellationToken ct = default)
    {
        var detail = await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");
        if (detail.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("La solicitud no pertenece al usuario autenticado.");
        return detail;
    }

    /// <inheritdoc/>
    public async Task<PagedEmployeeInternalRequestResult> GetPagedAsync(EmployeeInternalRequestQueryFilter filter, CancellationToken ct = default)
        => await _repository.GetPagedAsync(filter, ct);

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto> GetDetailByIdAsync(int requestId, CancellationToken ct = default)
        => await _repository.GetDetailByIdAsync(requestId, ct)
           ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto> ApproveAsync(int requestId, int reviewedBy, ReviewEmployeeInternalRequest request, CancellationToken ct = default)
        => await ChangeStatusAsync(requestId, reviewedBy, request, ReviewableStatuses,
            EmployeeInternalRequestStatus.Aprobado, "APPROVED", requireObservation: false,
            e => { }, ct);

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto> RejectAsync(int requestId, int reviewedBy, ReviewEmployeeInternalRequest request, CancellationToken ct = default)
        => await ChangeStatusAsync(requestId, reviewedBy, request, ReviewableStatuses,
            EmployeeInternalRequestStatus.Rechazado, "REJECTED", requireObservation: true,
            e => { }, ct);

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto> ReturnAsync(int requestId, int reviewedBy, ReviewEmployeeInternalRequest request, CancellationToken ct = default)
        => await ChangeStatusAsync(requestId, reviewedBy, request, ReviewableStatuses,
            EmployeeInternalRequestStatus.Devuelto, "RETURNED", requireObservation: true,
            e => { }, ct);

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto> CompleteAsync(int requestId, int resolvedBy, ReviewEmployeeInternalRequest request, CancellationToken ct = default)
        => await ChangeStatusAsync(requestId, resolvedBy,
            request, [EmployeeInternalRequestStatus.Aprobado],
            EmployeeInternalRequestStatus.Completado, "COMPLETED", requireObservation: false,
            e => { e.ResolvedAt = DateTime.Now; e.ResolvedBy = resolvedBy; }, ct);

    /// <inheritdoc/>
    public async Task<EmployeeInternalRequestDetailDto> HrCancelAsync(int requestId, int cancelledBy, CancelEmployeeInternalRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("El motivo de cancelación es obligatorio.", nameof(request));

        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (TerminalStatuses.Contains(entity.Status))
            throw new InvalidOperationException($"No se puede cancelar una solicitud en estado '{entity.Status}'.");

        EnsureRowVersionMatches(entity.RowVersion, request.RowVersion);

        var previousStatus = entity.Status;
        entity.Status = EmployeeInternalRequestStatus.Anulado;
        entity.CancelledAt = DateTime.Now;
        entity.CancelledBy = cancelledBy;

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new EmployeeInternalRequestStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = previousStatus,
            NewStatus = EmployeeInternalRequestStatus.Anulado,
            Action = "CANCELLED",
            Observation = request.Reason,
            CreatedAt = DateTime.Now,
            CreatedBy = cancelledBy
        }, ct);
        await _repository.SaveChangesAsync(ct);

        return await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud cancelada.");
    }

    // ── Helpers privados ──────────────────────────────────────────────────────────

    private async Task<EmployeeInternalRequestDetailDto> ChangeStatusAsync(
        int requestId,
        int actorId,
        ReviewEmployeeInternalRequest request,
        string[] allowedFromStatuses,
        string newStatus,
        string action,
        bool requireObservation,
        Action<EmployeeInternalRequest> applyExtraFields,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (requireObservation && string.IsNullOrWhiteSpace(request.Observation))
            throw new ArgumentException("La observación es obligatoria para esta acción.", nameof(request));

        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (!allowedFromStatuses.Contains(entity.Status))
            throw new InvalidOperationException($"No se puede realizar esta acción sobre una solicitud en estado '{entity.Status}'.");

        EnsureRowVersionMatches(entity.RowVersion, request.RowVersion);

        var previousStatus = entity.Status;
        entity.Status = newStatus;
        applyExtraFields(entity);

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new EmployeeInternalRequestStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Action = action,
            Observation = request.Observation,
            CreatedAt = DateTime.Now,
            CreatedBy = actorId
        }, ct);
        await _repository.SaveChangesAsync(ct);

        return await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud actualizada.");
    }

    private static void EnsureRowVersionMatches(byte[]? current, byte[]? incoming)
    {
        if (current is null || incoming is null || !current.SequenceEqual(incoming))
            throw new InvalidOperationException(
                "La solicitud fue modificada por otro proceso mientras tanto. Recarga los datos e intenta de nuevo.");
    }
}

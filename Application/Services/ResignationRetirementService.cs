using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.DTOs.PersonnelActions;
using WsUtaSystem.Application.DTOs.ResignationRetirement;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Gestión de solicitudes de renuncia y jubilación.
/// Flujo: PENDIENTE → (EN_REVISION) → APROBADO | RECHAZADO | DEVUELTO (→ PENDIENTE al reenviar) | ANULADO.
/// El EmployeeId de la solicitud siempre se resuelve desde el usuario autenticado — nunca se confía
/// en un valor enviado por el frontend.
/// </summary>
public sealed class ResignationRetirementService : IResignationRetirementService
{
    private static readonly string[] ActiveEditableStatuses =
    [
        ResignationRetirementStatus.Pendiente,
        ResignationRetirementStatus.Devuelto
    ];

    private static readonly string[] ReviewableStatuses =
    [
        ResignationRetirementStatus.Pendiente,
        ResignationRetirementStatus.EnRevision
    ];

    private static readonly string[] TerminalStatuses =
    [
        ResignationRetirementStatus.Aprobado,
        ResignationRetirementStatus.Rechazado,
        ResignationRetirementStatus.Anulado
    ];

    /// <summary>Plantillas separadas por tipo (ver Database/hr/13_resignation_retirement_templates_split.sql).</summary>
    private static string ResolveTemplateCode(string requestType) =>
        requestType == ResignationRetirementRequestType.Resignation ? "CARTA_RENUNCIA" : "CARTA_JUBILACION";

    /// <summary>Code de HR.tbl_personnel_action_type para la acción de desvinculación (ver Database\hr\12_resignation_separation_action.sql).</summary>
    private const string SeparationActionTypeCode = "RENUNCIA_JUBILACION";

    private static readonly string[] DocumentGenerableStatuses =
    [
        ResignationRetirementStatus.Pendiente,
        ResignationRetirementStatus.Devuelto
    ];

    private readonly IResignationRetirementRepository _repository;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly IParametersRepository _parametersRepository;
    private readonly IPersonnelActionService _personnelActionService;
    private readonly IEmailBuilder _emailBuilder;
    private readonly AppDbContext _db;
    private readonly ILogger<ResignationRetirementService> _logger;

    public ResignationRetirementService(
        IResignationRetirementRepository repository,
        IDocumentGenerationService documentGenerationService,
        IParametersRepository parametersRepository,
        IPersonnelActionService personnelActionService,
        IEmailBuilder emailBuilder,
        AppDbContext db,
        ILogger<ResignationRetirementService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _documentGenerationService = documentGenerationService ?? throw new ArgumentNullException(nameof(documentGenerationService));
        _parametersRepository = parametersRepository ?? throw new ArgumentNullException(nameof(parametersRepository));
        _personnelActionService = personnelActionService ?? throw new ArgumentNullException(nameof(personnelActionService));
        _emailBuilder = emailBuilder ?? throw new ArgumentNullException(nameof(emailBuilder));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Elegibilidad de jubilación: cumple con edad mínima O años de servicio mínimos,
    /// según HR.tbl_Parameters (RETIREMENT_MIN_AGE / RETIREMENT_MIN_SERVICE_YEARS).
    /// Es solo informativo — no bloquea la creación ni edición de la solicitud.
    /// </summary>
    private async Task<(bool Eligible, string? Note)> EvaluateRetirementEligibilityAsync(
        int? age, int serviceTimeYears, CancellationToken ct)
    {
        var minAge = await GetParameterIntAsync("RETIREMENT_MIN_AGE", 65, ct);
        var minServiceYears = await GetParameterIntAsync("RETIREMENT_MIN_SERVICE_YEARS", 30, ct);

        var byAge = age.HasValue && age.Value >= minAge;
        var byService = serviceTimeYears >= minServiceYears;

        if (byAge || byService)
        {
            return (true, null);
        }

        return (false,
            $"El empleado aún no alcanza los umbrales configurados para jubilación " +
            $"(edad mínima {minAge} años o {minServiceYears} años de servicio).");
    }

    private async Task<int> GetParameterIntAsync(string name, int defaultValue, CancellationToken ct)
    {
        var list = await _parametersRepository.GetByNameAsync(name, ct);
        var value = list?.FirstOrDefault()?.Pvalues;
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    /// <inheritdoc/>
    public async Task<EmployeeConsolidatedInfoDto> GetCurrentEmployeeInfoAsync(int employeeId, CancellationToken ct = default)
    {
        var info = await _repository.GetEmployeeConsolidatedInfoAsync(employeeId, ct)
                   ?? throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

        var (eligible, note) = await EvaluateRetirementEligibilityAsync(info.Age, info.ServiceTimeYears, ct);
        return info with { IsRetirementEligible = eligible, RetirementEligibilityNote = note };
    }

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> CreateAsync(int employeeId, CreateResignationRetirementRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (employeeId <= 0)
            throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

        if (request.RequestType != ResignationRetirementRequestType.Resignation
            && request.RequestType != ResignationRetirementRequestType.Retirement)
            throw new ArgumentException("RequestType debe ser RESIGNATION o RETIREMENT.", nameof(request));

        var employeeInfo = await _repository.GetEmployeeConsolidatedInfoAsync(employeeId, ct)
            ?? throw new InvalidOperationException("El usuario autenticado no tiene un empleado asociado en el sistema.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (request.ProposedExitDate < today)
            throw new InvalidOperationException("La fecha propuesta de salida no puede ser anterior a la fecha de solicitud.");

        if (await _repository.HasActiveRequestAsync(employeeId, request.RequestType, null, ct))
            throw new InvalidOperationException(
                $"Ya existe una solicitud de {request.RequestType} activa (pendiente, en revisión o devuelta) para este empleado.");

        if (request.RequestType == ResignationRetirementRequestType.Retirement)
        {
            var (eligible, note) = await EvaluateRetirementEligibilityAsync(employeeInfo.Age, employeeInfo.ServiceTimeYears, ct);
            if (!eligible)
                throw new InvalidOperationException(note);
        }

        var entity = new ResignationRetirementRequest
        {
            EmployeeId = employeeId,
            RequestType = request.RequestType,
            RequestDate = today,
            ProposedExitDate = request.ProposedExitDate,
            Reason = request.Reason,
            AdditionalNotes = request.AdditionalNotes,
            Status = ResignationRetirementStatus.Pendiente
        };

        await _repository.AddAsync(entity, ct);
        await _repository.SaveChangesAsync(ct); // necesario para obtener RequestId (identity)

        await _repository.AddHistoryAsync(new ResignationRetirementStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = null,
            NewStatus = ResignationRetirementStatus.Pendiente,
            Action = "CREATED",
            Observation = null,
            CreatedAt = DateTime.Now,
            CreatedBy = employeeId
        }, ct);
        await _repository.SaveChangesAsync(ct);

        _ = employeeInfo; // ya validado que existe; se usa en el detalle vía repositorio

        return await _repository.GetDetailByIdAsync(entity.RequestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud recién creada.");
    }

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> CreateOnBehalfAsync(
        int createdByEmployeeId, CreateResignationRetirementOnBehalfRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.EmployeeId <= 0)
            throw new ArgumentException("Debe indicar el empleado para el que se genera la solicitud.", nameof(request));

        if (request.RequestType != ResignationRetirementRequestType.Resignation
            && request.RequestType != ResignationRetirementRequestType.Retirement)
            throw new ArgumentException("RequestType debe ser RESIGNATION o RETIREMENT.", nameof(request));

        var employeeInfo = await _repository.GetEmployeeConsolidatedInfoAsync(request.EmployeeId, ct)
            ?? throw new InvalidOperationException("El empleado indicado no existe en el sistema.");

        if (await _repository.HasActiveRequestAsync(request.EmployeeId, request.RequestType, null, ct))
            throw new InvalidOperationException(
                $"Ya existe una solicitud de {request.RequestType} activa (pendiente, en revisión o devuelta) para este empleado.");

        if (request.RequestType == ResignationRetirementRequestType.Retirement)
        {
            var (eligible, note) = await EvaluateRetirementEligibilityAsync(employeeInfo.Age, employeeInfo.ServiceTimeYears, ct);
            if (!eligible)
                throw new InvalidOperationException(note);
        }

        var entity = new ResignationRetirementRequest
        {
            EmployeeId = request.EmployeeId,
            RequestType = request.RequestType,
            RequestDate = DateOnly.FromDateTime(DateTime.Today),
            // A diferencia de CreateAsync: aquí SÍ se permite una fecha ya pasada — el
            // objetivo es registrar cuanto antes una salida real ya ocurrida (empleado no
            // localizable, abandono de puesto) para que la acreditación mensual de
            // vacaciones se detenga sin esperar a que se complete el trámite formal con
            // documento firmado (ver recorte por ResignationExitDate en las SP de acreditación).
            ProposedExitDate = request.ProposedExitDate,
            Reason = request.Reason,
            AdditionalNotes = request.AdditionalNotes,
            Status = ResignationRetirementStatus.Pendiente
        };

        await _repository.AddAsync(entity, ct);
        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new ResignationRetirementStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = null,
            NewStatus = ResignationRetirementStatus.Pendiente,
            Action = "CREATED_BY_HR",
            Observation = "Solicitud generada por Recursos Humanos en representación del empleado (no la creó el propio empleado).",
            CreatedAt = DateTime.Now,
            CreatedBy = createdByEmployeeId
        }, ct);
        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ResignationRetirementService: solicitud {RequestId} generada por RRHH (EmployeeId={CreatedBy}) en nombre de EmployeeId={TargetEmployeeId}.",
            entity.RequestId, createdByEmployeeId, request.EmployeeId);

        return await _repository.GetDetailByIdAsync(entity.RequestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud recién creada.");
    }

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> UpdateAsync(int requestId, int employeeId, UpdateResignationRetirementRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (entity.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("La solicitud no pertenece al usuario autenticado.");

        if (!ActiveEditableStatuses.Contains(entity.Status))
            throw new InvalidOperationException($"No se puede editar una solicitud en estado '{entity.Status}'.");

        EnsureRowVersionMatches(entity.RowVersion, request.RowVersion);

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (request.ProposedExitDate < today)
            throw new InvalidOperationException("La fecha propuesta de salida no puede ser anterior a la fecha de solicitud.");

        var previousStatus = entity.Status;
        var wasReturned = entity.Status == ResignationRetirementStatus.Devuelto;

        entity.ProposedExitDate = request.ProposedExitDate;
        entity.Reason = request.Reason;
        entity.AdditionalNotes = request.AdditionalNotes;
        if (wasReturned)
            entity.Status = ResignationRetirementStatus.Pendiente;

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new ResignationRetirementStatusHistory
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
    public async Task CancelOwnAsync(int requestId, int employeeId, CancelResignationRetirementRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("El motivo de cancelación es obligatorio.", nameof(request));

        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (entity.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("La solicitud no pertenece al usuario autenticado.");

        if (TerminalStatuses.Contains(entity.Status))
            throw new InvalidOperationException($"No se puede cancelar una solicitud en estado '{entity.Status}'.");

        EnsureRowVersionMatches(entity.RowVersion, request.RowVersion);

        var previousStatus = entity.Status;
        entity.Status = ResignationRetirementStatus.Anulado;
        entity.CancelledAt = DateTime.Now;
        entity.CancelledBy = employeeId;

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new ResignationRetirementStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = previousStatus,
            NewStatus = ResignationRetirementStatus.Anulado,
            Action = "CANCELLED",
            Observation = request.Reason,
            CreatedAt = DateTime.Now,
            CreatedBy = employeeId
        }, ct);
        await _repository.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<PagedResignationRetirementResult> GetMyRequestsAsync(int employeeId, ResignationRetirementQueryFilter filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        // Se fuerza el EmployeeId sin importar lo que llegue en el filtro — "mis solicitudes"
        // nunca puede mostrar las de otro empleado.
        var ownFilter = filter with { EmployeeId = employeeId, AllowedDepartmentIds = null };
        return await _repository.GetPagedAsync(ownFilter, ct);
    }

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> GetMyRequestDetailAsync(int requestId, int employeeId, CancellationToken ct = default)
    {
        var detail = await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (detail.Employee.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("La solicitud no pertenece al usuario autenticado.");

        return detail;
    }

    /// <inheritdoc/>
    public async Task<PagedResignationRetirementResult> GetPagedAsync(ResignationRetirementQueryFilter filter, CancellationToken ct = default)
        => await _repository.GetPagedAsync(filter, ct);

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> GetDetailByIdAsync(int requestId, CancellationToken ct = default)
        => await _repository.GetDetailByIdAsync(requestId, ct)
           ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> ApproveAsync(int requestId, int reviewedBy, ReviewResignationRetirementRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (!ReviewableStatuses.Contains(entity.Status))
            throw new InvalidOperationException($"No se puede aprobar una solicitud en estado '{entity.Status}'.");

        EnsureRowVersionMatches(entity.RowVersion, request.RowVersion);

        var employeeInfo = await _repository.GetEmployeeConsolidatedInfoAsync(entity.EmployeeId, ct)
            ?? throw new InvalidOperationException("No se encontró información del empleado.");
        if (employeeInfo.VigenteSourceType is null)
            throw new InvalidOperationException(
                "No se puede aprobar: el empleado no tiene un contrato, nombramiento o acción de personal vigente.");

        var previousStatus = entity.Status;
        entity.Status = ResignationRetirementStatus.Aprobado;
        entity.ApprovedAt = DateTime.Now;
        entity.ApprovedBy = reviewedBy;

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new ResignationRetirementStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = previousStatus,
            NewStatus = ResignationRetirementStatus.Aprobado,
            Action = "APPROVED",
            Observation = request.Observation,
            CreatedAt = DateTime.Now,
            CreatedBy = reviewedBy
        }, ct);
        await _repository.SaveChangesAsync(ct);

        return await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud aprobada.");
    }

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> UploadSignedDocumentAsync(int requestId, int reviewedBy, ApproveResignationRetirementRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (!ReviewableStatuses.Contains(entity.Status))
            throw new InvalidOperationException($"No se puede aprobar una solicitud en estado '{entity.Status}'.");

        EnsureRowVersionMatches(entity.RowVersion, request.RowVersion);

        // El documento firmado es obligatorio para aprobar: debe existir y pertenecer
        // exactamente a esta solicitud (no al de otro empleado/módulo).
        if (!await _repository.StoredFileBelongsToRequestAsync(requestId, request.StoredFileId, ct))
            throw new InvalidOperationException(
                "El documento indicado no está adjunto a esta solicitud. Suba el documento firmado antes de aprobar.");

        var employeeInfo = await _repository.GetEmployeeConsolidatedInfoAsync(entity.EmployeeId, ct)
            ?? throw new InvalidOperationException("No se encontró información del empleado.");
        if (employeeInfo.VigenteSourceType is null)
            throw new InvalidOperationException(
                "No se puede aprobar: el empleado no tiene un contrato, nombramiento o acción de personal vigente.");

        var actionTypeId = await _db.PersonnelActionTypes
            .AsNoTracking()
            .Where(t => t.Code == SeparationActionTypeCode)
            .Select(t => (int?)t.PersonnelActionTypeId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                $"No existe el tipo de acción de personal '{SeparationActionTypeCode}' — no se puede generar la acción de desvinculación.");

        var personId = employeeInfo.PersonId
            ?? throw new InvalidOperationException("El empleado no tiene una persona asociada en el sistema.");

        // Contrato a cerrar (si el vigente del empleado es un contrato, no un nombramiento/acción)
        var contractId = employeeInfo.VigenteSourceType == "CONTRACT" ? employeeInfo.VigenteSourceId : null;

        var createResponse = await _personnelActionService.CreateAsync(
            new CreatePersonnelActionRequest(
                personId: personId,
                EmployeeId: entity.EmployeeId,
                ActionTypeId: actionTypeId,
                ActionNumber: null,
                ActionDate: DateOnly.FromDateTime(DateTime.Today),
                EffectiveDate: entity.ProposedExitDate,
                EndDate: null,
                OriginDepartmentId: employeeInfo.DepartmentId,
                OriginJobId: null,
                OriginBudgetCode: null,
                DestinationDepartmentId: null,
                DestinationJobId: null,
                DestinationBudgetCode: null,
                PreviousRmu: null,
                NewRmu: null,
                LegalBasis: null,
                Reason: entity.Reason,
                Observations: request.Observation,
                ContractId: contractId,
                MovementId: null,
                EmployeeTypeId: null,
                SwornDeclaration: false,
                InstitutionalProcess: null,
                ManagementLevel: null,
                DthDirectorId: null,
                AuthorityNominatorId: null,
                ElaboratorId: null,
                ReviewerId: null,
                RegistrarId: null,
                GenerateDocument: true,
                DocumentOverrides: null),
            reviewedBy, ct);

        if (createResponse.GeneratedDocumentId is null)
            throw new InvalidOperationException(
                "No se pudo generar el documento de la acción de personal de desvinculación — revise que el tipo RENUNCIA_JUBILACION tenga una plantilla publicada asignada.");

        var actionId = createResponse.ActionId;

        await _personnelActionService.MarkPendingSignaturesAsync(actionId, null, reviewedBy, ct);

        // Dispara: transición a FIRMADO_CARGADO, deshabilitación de la cuenta institucional
        // (RequiresAdUserDisable=1) y, si hay ContractId, cierre del contrato a RENUNCIA —
        // ver PersonnelActionService.UploadSignedDocumentAsync/TriggerContractSeparationAsync.
        await _personnelActionService.UploadSignedDocumentAsync(
            actionId, new UploadSignedDocumentRequest(request.StoredFileId, request.Observation), reviewedBy, ct);

        var previousStatus = entity.Status;
        entity.LinkedPersonnelActionId = actionId;
        entity.Status = ResignationRetirementStatus.Aprobado;
        entity.ApprovedAt = DateTime.Now;
        entity.ApprovedBy = reviewedBy;

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new ResignationRetirementStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = previousStatus,
            NewStatus = ResignationRetirementStatus.Aprobado,
            Action = "SIGNED_UPLOADED",
            Observation = request.Observation,
            CreatedAt = DateTime.Now,
            CreatedBy = reviewedBy
        }, ct);
        await _repository.SaveChangesAsync(ct);

        // Destinatario: SOLO el correo institucional (Employees.Email). Nunca se usa el correo
        // personal de People/tbl_Person — ese campo no es el canal oficial para esta notificación.
        var institutionalEmail = await _db.Employees.AsNoTracking()
            .Where(e => e.EmployeeId == entity.EmployeeId)
            .Select(e => e.Email)
            .FirstOrDefaultAsync(ct);
        var toEmail = institutionalEmail?.Trim();

        if (!string.IsNullOrWhiteSpace(toEmail))
        {
            var isResignation = entity.RequestType == ResignationRetirementRequestType.Resignation;
            var typeLabelLower = isResignation ? "renuncia" : "jubilación";
            var approvedDate = (entity.ApprovedAt ?? DateTime.Now).ToString("dd/MM/yyyy");

            var body =
                $"<p>Estimado/a {employeeInfo.FullName}:</p>" +
                $"<p>Se informa que su solicitud de {typeLabelLower} ha sido recibida y revisada por la Dirección de Talento Humano.</p>" +
                $"<p>La solicitud se encuentra aprobada con fecha {approvedDate}.</p>" +
                $"<p>Referencia: Solicitud N.° {requestId}</p>" +
                "<p>Este mensaje corresponde a una notificación automática del sistema.</p>" +
                "<p>Atentamente,<br/>Dirección de Talento Humano</p>";

            await _emailBuilder.TryNotifyAsync(
                EmailTemplateKey.ResignationRetirementApproved,
                $"Solicitud de {typeLabelLower} aprobada",
                body,
                to: toEmail, ct: ct);
        }
        else
        {
            _logger.LogWarning(
                "ResignationRetirementService: solicitud {RequestId} aprobada pero el empleado {EmployeeId} no tiene correo institucional registrado — no se envió notificación.",
                requestId, entity.EmployeeId);
        }

        _logger.LogInformation(
            "ResignationRetirementService: solicitud {RequestId} aprobada con documento firmado, acción de personal {ActionId} vinculada.",
            requestId, actionId);

        return await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud aprobada.");
    }

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> RejectAsync(int requestId, int reviewedBy, ReviewResignationRetirementRequest request, CancellationToken ct = default)
        => await ChangeStatusWithMandatoryObservationAsync(
            requestId, reviewedBy, request, ReviewableStatuses,
            ResignationRetirementStatus.Rechazado, "REJECTED",
            (e) => { e.RejectedAt = DateTime.Now; e.RejectedBy = reviewedBy; }, ct);

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> ReturnAsync(int requestId, int reviewedBy, ReviewResignationRetirementRequest request, CancellationToken ct = default)
        => await ChangeStatusWithMandatoryObservationAsync(
            requestId, reviewedBy, request, ReviewableStatuses,
            ResignationRetirementStatus.Devuelto, "RETURNED",
            _ => { }, ct);

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> HrCancelAsync(int requestId, int cancelledBy, CancelResignationRetirementRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("El motivo de cancelación es obligatorio.", nameof(request));

        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (entity.Status is ResignationRetirementStatus.Rechazado or ResignationRetirementStatus.Anulado)
            throw new InvalidOperationException($"No se puede cancelar una solicitud en estado '{entity.Status}'.");

        EnsureRowVersionMatches(entity.RowVersion, request.RowVersion);

        var previousStatus = entity.Status;
        entity.Status = ResignationRetirementStatus.Anulado;
        entity.CancelledAt = DateTime.Now;
        entity.CancelledBy = cancelledBy;

        await _repository.SaveChangesAsync(ct);

        await _repository.AddHistoryAsync(new ResignationRetirementStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = previousStatus,
            NewStatus = ResignationRetirementStatus.Anulado,
            Action = "CANCELLED",
            Observation = request.Reason,
            CreatedAt = DateTime.Now,
            CreatedBy = cancelledBy
        }, ct);
        await _repository.SaveChangesAsync(ct);

        return await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud cancelada.");
    }

    /// <inheritdoc/>
    public async Task<ResignationRetirementDetailDto> GenerateDocumentAsync(int requestId, int employeeId, CancellationToken ct = default)
    {
        var entity = await _repository.GetTrackedByIdAsync(requestId, ct)
            ?? throw new KeyNotFoundException($"No existe la solicitud {requestId}.");

        if (entity.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("La solicitud no pertenece al usuario autenticado.");

        if (!DocumentGenerableStatuses.Contains(entity.Status))
            throw new InvalidOperationException(
                $"No se puede generar el documento de una solicitud en estado '{entity.Status}' — ya fue firmada/cargada o resuelta.");

        var employeeInfo = await _repository.GetEmployeeConsolidatedInfoAsync(employeeId, ct)
            ?? throw new InvalidOperationException("No se encontró información del empleado.");

        var templateCode = ResolveTemplateCode(entity.RequestType);
        var templateId = await _repository.GetPublishedTemplateIdAsync(templateCode, ct)
            ?? throw new InvalidOperationException($"No existe una plantilla publicada '{templateCode}'.");

        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["JOB_DESCRIPTION"] = employeeInfo.JobTitle ?? string.Empty,
            ["DEPARTMENT_NAME"] = employeeInfo.DepartmentName ?? string.Empty,
            ["PROPOSED_EXIT_DATE"] = entity.ProposedExitDate.ToString("dd/MM/yyyy"),
            ["REASON"] = entity.Reason ?? string.Empty
        };

        var generated = await _documentGenerationService.GenerateAsync(
            new GenerateDocumentRequest(
                TemplateId: templateId,
                EmployeeId: employeeId,
                EntityType: DocumentEntityType.ResignationRetirement,
                EntityId: entity.RequestId,
                DocumentNumber: null,
                Notes: null,
                ManualOverrides: overrides),
            employeeId,
            ct);

        entity.GeneratedDocumentId = generated.DocumentId;
        await _repository.SaveChangesAsync(ct);

        return await _repository.GetDetailByIdAsync(requestId, ct)
            ?? throw new InvalidOperationException("No se pudo recuperar la solicitud tras generar el documento.");
    }

    /// <inheritdoc/>
    public async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadMyDocumentAsync(int requestId, int employeeId, CancellationToken ct = default)
    {
        var detail = await GetMyRequestDetailAsync(requestId, employeeId, ct);
        return await DownloadResolvedAsync(detail, ct);
    }

    /// <inheritdoc/>
    public async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadDocumentAsync(int requestId, CancellationToken ct = default)
    {
        var detail = await GetDetailByIdAsync(requestId, ct);
        return await DownloadResolvedAsync(detail, ct);
    }

    private async Task<(byte[] Bytes, string FileName, string ContentType)> DownloadResolvedAsync(ResignationRetirementDetailDto detail, CancellationToken ct)
    {
        if (detail.GeneratedDocumentId is null)
            throw new InvalidOperationException("Esta solicitud todavía no tiene un documento generado.");

        return await _documentGenerationService.DownloadAsync(detail.GeneratedDocumentId.Value, ct);
    }

    // ── Helpers privados ──────────────────────────────────────────────────────────

    private async Task<ResignationRetirementDetailDto> ChangeStatusWithMandatoryObservationAsync(
        int requestId,
        int reviewedBy,
        ReviewResignationRetirementRequest request,
        string[] allowedFromStatuses,
        string newStatus,
        string action,
        Action<ResignationRetirementRequest> applyExtraFields,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Observation))
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

        await _repository.AddHistoryAsync(new ResignationRetirementStatusHistory
        {
            RequestId = entity.RequestId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Action = action,
            Observation = request.Observation,
            CreatedAt = DateTime.Now,
            CreatedBy = reviewedBy
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

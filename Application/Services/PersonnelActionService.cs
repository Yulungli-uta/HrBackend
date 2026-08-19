using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.DTOs.PersonnelActions;
using WsUtaSystem.Application.DTOs.Provisioning;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Gestión de acciones de personal (LOSEP/RLOSEP).
/// Flujo: BORRADOR → GENERADO → PENDIENTE_FIRMAS → FIRMADO_CARGADO → FINALIZADO
/// Cancelación disponible desde cualquier estado excepto FINALIZADO.
/// </summary>
public sealed class PersonnelActionService : IPersonnelActionService
{
    // Grafo de transiciones válidas por estado origen
    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedTransitions =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["BORRADOR"]         = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GENERADO", "ANULADO" },
            ["GENERADO"]         = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PENDIENTE_FIRMAS", "BORRADOR", "ANULADO" },
            ["PENDIENTE_FIRMAS"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FIRMADO_CARGADO", "GENERADO", "ANULADO" },
            // 2026-07-06: VIGENTE agregado — solo para tipos con ReachesVigente=1
            // (Nombramiento, Traslado, Encargo, Cambio de Sueldo, Asistencia/Horario).
            // La transición FIRMADO_CARGADO->VIGENTE es automática al cargar el
            // documento firmado (ver UploadSignedDocumentAsync), no un paso manual.
            ["FIRMADO_CARGADO"]  = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FINALIZADO", "VIGENTE", "ANULADO" },
            ["VIGENTE"]          = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FINALIZADO" },
            ["FINALIZADO"]       = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            ["ANULADO"]          = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            // Compatibilidad con registros históricos creados antes de la migración de estados
            ["DRAFT"]            = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BORRADOR", "GENERADO", "ANULADO" },
            ["APPROVED"]         = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GENERADO", "PENDIENTE_FIRMAS" },
            ["EXECUTED"]         = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FINALIZADO" },
            ["CANCELLED"]        = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            // C# 12 collection expressions no se usan para evitar ambigüedad — conjuntos vacíos con constructor explícito
        };

    private readonly IPersonnelActionRepository _actionRepository;
    private readonly IPersonnelActionTypeRepository _personnelActionType;
    private readonly IEmployeesRepository _employeesRepository;
    private readonly IDocumentTemplateRepository _templateRepository;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly IHttpContextAccessor _httpContext;
    private readonly AppDbContext _db;
    private readonly ILogger<PersonnelActionService> _logger;
    // Orquestador reutilizable: centraliza EnsureEmployee + RepositoryUta + UpdateEmail + SendEmail
    private readonly IEmployeeProvisioningOrchestrator _provisioningOrchestrator;
    private readonly IEmployeeProvisioningClient _provisioningClient;
    private readonly IEmployeeLaborRegimeService _laborRegimeService;
    private readonly IPersonnelMovementsService _movementsService;
    private readonly IContractsService _contractsService;
    private readonly ICurrentUserService _currentUser;
    private readonly ISalaryHistoryService _salaryHistory;

    /// <summary>
    /// Código de PersonnelActionType cuyo efecto colateral, al cargar el documento firmado,
    /// además de RequiresAdUserDisable, cierra el contrato vigente asociado (si lo hay) a
    /// RENUNCIA. Ver <see cref="TriggerContractSeparationAsync"/>.
    /// </summary>
    private const string ResignationRetirementActionTypeCode = "RENUNCIA_JUBILACION";

    public PersonnelActionService(
        IPersonnelActionRepository actionRepository,
        IPersonnelActionTypeRepository personnelActionType,
        IEmployeesRepository employeesRepository,
        IDocumentTemplateRepository templateRepository,
        IDocumentGenerationService documentGenerationService,
        IHttpContextAccessor httpContext,
        AppDbContext db,
        ILogger<PersonnelActionService> logger,
        IEmployeeProvisioningOrchestrator provisioningOrchestrator,
        IEmployeeProvisioningClient provisioningClient,
        IEmployeeLaborRegimeService laborRegimeService,
        IPersonnelMovementsService movementsService,
        IContractsService contractsService,
        ICurrentUserService currentUser,
        ISalaryHistoryService salaryHistory)
    {
        _actionRepository          = actionRepository;
        _personnelActionType       = personnelActionType;
        _employeesRepository       = employeesRepository;
        _templateRepository        = templateRepository;
        _documentGenerationService = documentGenerationService;
        _httpContext               = httpContext;
        _db                        = db;
        _logger                    = logger;
        _provisioningOrchestrator  = provisioningOrchestrator;
        _provisioningClient        = provisioningClient;
        _movementsService          = movementsService;
        _laborRegimeService        = laborRegimeService;
        _contractsService          = contractsService;
        _currentUser               = currentUser;
        _salaryHistory             = salaryHistory;
    }

    /// <inheritdoc />
    public async Task<PagedPersonnelActionResult> GetPagedAsync(
        PersonnelActionQueryFilter filter,
        CancellationToken ct = default)
        => await _actionRepository.GetPagedAsync(filter, ct);

    /// <inheritdoc />
    public async Task<PersonnelActionDetailDto> GetDetailByIdAsync(
        int actionId,
        CancellationToken ct = default)
        => await _actionRepository.GetDetailByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonnelActionSummaryDto>> GetByEmployeeIdAsync(
        int employeeId,
        CancellationToken ct = default)
        => await _actionRepository.GetByEmployeeIdAsync(employeeId, ct);

    /// <inheritdoc />
    public async Task<CreatePersonnelActionResponse> CreateAsync(
        CreatePersonnelActionRequest request,
        int createdBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsHistoricalEntry)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (request.ActionDate >= today)
                throw new ArgumentException("Un registro histórico debe tener fecha de acción anterior a hoy.");
            if (request.EffectiveDate is { } effectiveDate && effectiveDate >= today)
                throw new ArgumentException("Un registro histórico debe tener fecha de vigencia anterior a hoy.");
            if (request.EndDate is { } endDate && endDate.Year < 9999 && endDate >= today)
                throw new ArgumentException("Un registro histórico debe tener fecha de fin anterior a hoy.");
        }

        int personId = request.personId;
        var employee = await _employeesRepository.GetByPersonIdAsync(personId, ct);

        // Reservar el número de acción de forma atómica desde la secuencia del tipo — salvo
        // en registro histórico con número manual: el usuario ya tiene el número real del
        // documento que existía antes de este sistema, no se le genera uno nuevo.
        string reservedNumber;
        if (request.IsHistoricalEntry && !string.IsNullOrWhiteSpace(request.ActionNumber))
        {
            reservedNumber = request.ActionNumber.Trim();
            await ValidateActionNumberUniqueAsync(0, reservedNumber, ct);
        }
        else
        {
            (reservedNumber, _, _) = await _personnelActionType
                .ConsumeNextNumberAsync(request.ActionTypeId, DateTime.Now.Year, ct);
        }

        var action = new PersonnelAction
        {
            PersonId                = request.personId,
            EmployeeId              = (request.EmployeeId is > 0) ? request.EmployeeId : null,
            ActionTypeId            = request.ActionTypeId,
            ActionNumber            = reservedNumber,
            ActionDate              = request.ActionDate,
            EffectiveDate           = request.EffectiveDate,
            EndDate                 = request.EndDate,
            OriginDepartmentId      = request.OriginDepartmentId,
            OriginJobId             = request.OriginJobId,
            OriginBudgetCode        = request.OriginBudgetCode?.Trim(),
            DestinationDepartmentId = request.DestinationDepartmentId,
            DestinationJobId        = request.DestinationJobId,
            DestinationBudgetCode   = request.DestinationBudgetCode?.Trim(),
            PreviousRmu             = request.PreviousRmu,
            NewRmu                  = request.NewRmu,
            LegalBasis              = request.LegalBasis?.Trim(),
            Reason                  = request.Reason?.Trim(),
            Observations            = request.Observations?.Trim(),
            ContractId              = request.ContractId,
            MovementId              = request.MovementId,
            EmployeeTypeId          = request.EmployeeTypeId is > 0 ? request.EmployeeTypeId : null,
            SwornDeclaration        = request.SwornDeclaration,
            InstitutionalProcess    = request.InstitutionalProcess,
            ManagementLevel        = request.ManagementLevel,
            DthDirectorId           = request.DthDirectorId,
            AuthorityNominatorId    = request.AuthorityNominatorId,
            ElaboratorId            = request.ElaboratorId,
            ReviewerId              = request.ReviewerId,
            RegistrarId             = request.RegistrarId,
            Status                  = "BORRADOR",
            CreatedAt               = DateTime.UtcNow,
            CreatedBy               = createdBy
        };

        var actionId = await _actionRepository.CreateAsync(action, ct);

        await WriteHistoryAsync(actionId, fromStatus: null, toStatus: "BORRADOR", "Acción de personal creada.", createdBy, ct);

        _logger.LogInformation("PersonnelActionService: acción {ActionId} creada en estado BORRADOR.", actionId);

        if (!request.GenerateDocument)
        {
            return new CreatePersonnelActionResponse(
                ActionId: actionId, ActionNumber: reservedNumber,
                Status: "BORRADOR", GeneratedDocumentId: null,
                PdfBase64: null, FileName: null);
        }

        var docResponse = await GenerateDocumentForActionAsync(
            actionId, request.EmployeeId, request.personId, request.ContractId,
            request.DocumentOverrides, createdBy, ct);

        if (docResponse is not null)
            await TransitionStatusAsync(actionId, "BORRADOR", "GENERADO",
                "Documento generado al crear la acción.", createdBy, ct);

        return new CreatePersonnelActionResponse(
            ActionId: actionId, ActionNumber: reservedNumber,
            Status: docResponse is not null ? "GENERADO" : "BORRADOR",
            GeneratedDocumentId: docResponse?.DocumentId,
            PdfBase64: docResponse?.PdfBase64,
            FileName: docResponse?.FileName);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        int actionId,
        UpdatePersonnelActionRequest request,
        int updatedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var action = await _actionRepository.GetByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

        var editableStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "BORRADOR", "GENERADO", "DRAFT" };

        if (!editableStatuses.Contains(action.Status))
            throw new InvalidOperationException(
                $"Solo se puede editar en estado BORRADOR o GENERADO. Estado actual: '{action.Status}'.");

        action.ActionNumber            = request.ActionNumber?.Trim();
        action.ActionDate              = request.ActionDate;
        action.EffectiveDate           = request.EffectiveDate;
        action.EndDate                 = request.EndDate;
        action.OriginDepartmentId      = request.OriginDepartmentId;
        action.OriginJobId             = request.OriginJobId;
        action.OriginBudgetCode        = request.OriginBudgetCode?.Trim();
        action.DestinationDepartmentId = request.DestinationDepartmentId;
        action.DestinationJobId        = request.DestinationJobId;
        action.DestinationBudgetCode   = request.DestinationBudgetCode?.Trim();
        action.PreviousRmu             = request.PreviousRmu;
        action.NewRmu                  = request.NewRmu;
        action.LegalBasis              = request.LegalBasis?.Trim();
        action.Reason                  = request.Reason?.Trim();
        action.Observations            = request.Observations?.Trim();
        action.SwornDeclaration        = request.SwornDeclaration;
        action.InstitutionalProcess    = request.InstitutionalProcess;
        action.ManagementLevel         = request.ManagementLevel;
        action.EmployeeTypeId          = request.EmployeeTypeId is > 0 ? request.EmployeeTypeId : action.EmployeeTypeId;
        action.DthDirectorId           = request.DthDirectorId;
        action.AuthorityNominatorId    = request.AuthorityNominatorId;
        action.ElaboratorId            = request.ElaboratorId;
        action.ReviewerId              = request.ReviewerId;
        action.RegistrarId             = request.RegistrarId;
        action.UpdatedAt               = DateTime.UtcNow;
        action.UpdatedBy               = updatedBy;

        await _actionRepository.UpdateAsync(action, ct);
    }

    /// <summary>
    /// Registra/actualiza en <c>HR.tbl_SalaryHistory</c> el sueldo de la acción indicada
    /// (documento fuente = este <see cref="PersonnelAction.ActionId"/>). No hace nada si la
    /// acción no tiene <see cref="PersonnelAction.NewRmu"/> o <see cref="PersonnelAction.EmployeeId"/>
    /// cargados — el disparo automático (firma) y la corrección manual comparten esta misma
    /// lógica de upsert.
    /// </summary>
    private async Task RecordSalaryHistoryForActionAsync(PersonnelAction action, string reason, CancellationToken ct)
    {
        if (!action.NewRmu.HasValue || !action.EmployeeId.HasValue)
            return;

        await _salaryHistory.UpsertForActionAsync(
            action.ActionId, action.EmployeeId.Value, action.PreviousRmu ?? 0m, action.NewRmu.Value,
            _currentUser.UserName ?? _currentUser.Email ?? "system", reason, ct);
    }

    /// <summary>
    /// ActionNumber ya está respaldado por índice único (UQ_PersonnelActions_ActionNumber),
    /// pero se valida aquí antes para dar un mensaje claro en vez del 409 genérico de
    /// violación de índice — relevante ahora que la corrección permite editarlo.
    /// </summary>
    private async Task ValidateActionNumberUniqueAsync(int actionId, string? actionNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(actionNumber)) return;

        var duplicateId = await _db.Set<PersonnelAction>()
            .Where(a => a.ActionNumber == actionNumber && a.ActionId != actionId)
            .Select(a => (int?)a.ActionId)
            .FirstOrDefaultAsync(ct);

        if (duplicateId.HasValue)
            throw new BusinessRuleException(
                $"El número de acción '{actionNumber}' ya está en uso por la acción #{duplicateId.Value}. Verifique el número de documento.");
    }

    /// <inheritdoc />
    public async Task CorrectAsync(
        int actionId,
        UpdatePersonnelActionRequest request,
        string reason,
        int correctedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Debe ingresar el motivo de la corrección.", nameof(reason));

        var action = await _actionRepository.GetByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

        var before = AuditSnapshotHelper.Snapshot(action);

        // A diferencia de UpdateAsync, la corrección se permite en cualquier estado (incluido
        // VIGENTE/FINALIZADO) — exige motivo obligatorio y queda auditada en HR.Audit.
        var newActionNumber = request.ActionNumber?.Trim();
        await ValidateActionNumberUniqueAsync(actionId, newActionNumber, ct);
        action.ActionNumber            = newActionNumber;
        action.ActionDate              = request.ActionDate;
        action.EffectiveDate           = request.EffectiveDate;
        action.EndDate                 = request.EndDate;
        action.OriginDepartmentId      = request.OriginDepartmentId;
        action.OriginJobId             = request.OriginJobId;
        action.OriginBudgetCode        = request.OriginBudgetCode?.Trim();
        action.DestinationDepartmentId = request.DestinationDepartmentId;
        action.DestinationJobId        = request.DestinationJobId;
        action.DestinationBudgetCode   = request.DestinationBudgetCode?.Trim();
        action.PreviousRmu             = request.PreviousRmu;
        action.NewRmu                  = request.NewRmu;
        action.LegalBasis              = request.LegalBasis?.Trim();
        action.Reason                  = request.Reason?.Trim();
        action.Observations            = request.Observations?.Trim();
        action.SwornDeclaration        = request.SwornDeclaration;
        action.InstitutionalProcess    = request.InstitutionalProcess;
        action.ManagementLevel         = request.ManagementLevel;
        action.EmployeeTypeId          = request.EmployeeTypeId is > 0 ? request.EmployeeTypeId : action.EmployeeTypeId;
        action.DthDirectorId           = request.DthDirectorId;
        action.AuthorityNominatorId    = request.AuthorityNominatorId;
        action.ElaboratorId            = request.ElaboratorId;
        action.ReviewerId              = request.ReviewerId;
        action.RegistrarId             = request.RegistrarId;
        action.UpdatedAt               = DateTime.UtcNow;
        action.UpdatedBy               = correctedBy;

        await _actionRepository.UpdateAsync(action, ct);

        // Corrige (o crea si aún no existía) la fila de SalaryHistory ligada
        // específicamente a ESTA acción — nunca la de otro contrato/acción
        // del mismo empleado.
        await RecordSalaryHistoryForActionAsync(action, $"Corrección de acción de personal: {reason}", ct);

        var after = AuditSnapshotHelper.Snapshot(action);
        await AuditSnapshotHelper.WriteCorrectionAuditAsync(
            _db, "PersonnelActions", actionId.ToString(), reason, before, after,
            _currentUser.UserName ?? _currentUser.Email, ct);

        _logger.LogInformation(
            "PersonnelActionService: acción {ActionId} CORREGIDA por EmployeeId={UserId}. Motivo: {Reason}",
            actionId, correctedBy, reason);
    }

    /// <inheritdoc />
    public async Task<CreatePersonnelActionResponse> ApproveAsync(
        int actionId,
        ApprovePersonnelActionRequest request,
        int approvedBy,
        CancellationToken ct = default)
    {
        // Compatibilidad con el endpoint /approve existente.
        // En el nuevo flujo, "aprobar" equivale a generar el documento (BORRADOR → GENERADO).
        ArgumentNullException.ThrowIfNull(request);

        var action = await _actionRepository.GetByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

        if (action.EmployeeId == approvedBy)
            throw new InvalidOperationException("No puede aprobar una acción de personal de la cual usted es el empleado afectado.");

        GenerateDocumentResponse? docResponse = null;

        if (request.GenerateDocumentIfMissing && !action.GeneratedDocumentId.HasValue)
        {
            docResponse = await GenerateDocumentForActionAsync(
                actionId, action.EmployeeId, action.PersonId, action.ContractId,
                overrides: null, approvedBy, ct);
        }

        var finalStatus = docResponse is not null ? "GENERADO" : action.Status;

        return new CreatePersonnelActionResponse(
            ActionId: actionId, ActionNumber: action.ActionNumber,
            Status: finalStatus,
            GeneratedDocumentId: docResponse?.DocumentId ?? action.GeneratedDocumentId,
            PdfBase64: docResponse?.PdfBase64,
            FileName: docResponse?.FileName);
    }

    /// <inheritdoc />
    public async Task<CreatePersonnelActionResponse> GenerateDocumentAsync(
        int actionId,
        Dictionary<string, string>? overrides,
        int generatedBy,
        CancellationToken ct = default)
    {
        var action = await _actionRepository.GetByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

        // 2026-07-06: FIRMADO_CARGADO agregado — el frontend ya oculta el botón de
        // generar/regenerar a partir de ese estado, pero el backend no lo exigía. Un
        // documento ya firmado y cargado no debe poder regenerarse (sobreescribiría el
        // registro de un documento que ya tiene una firma física real asociada).
        // 2026-07-06: VIGENTE agregado — nuevo estado que tampoco debe permitir
        // regenerar el documento (mismo motivo que FIRMADO_CARGADO/FINALIZADO).
        var blockedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "FIRMADO_CARGADO", "VIGENTE", "FINALIZADO", "ANULADO", "CANCELLED" };

        if (blockedStatuses.Contains(action.Status))
            throw new InvalidOperationException(
                $"No se puede generar documento en estado '{action.Status}'.");

        var docResponse = await GenerateDocumentForActionAsync(
            actionId, action.EmployeeId, action.PersonId, action.ContractId,
            overrides, generatedBy, ct);

        // A diferencia de CreateAsync/ApproveAsync (donde generar el documento es un paso
        // opcional dentro de otra operación principal), este método SOLO existe para generar
        // el documento — si no se pudo generar (sin plantilla, plantilla no publicada, etc.),
        // no hay nada más que ofrecer como "éxito". Antes esto devolvía 200 igual, con el
        // estado sin cambiar, y el frontend mostraba "Documento generado" aunque no se generó
        // nada (bug reportado 2026-08-18).
        if (docResponse is null)
            throw new BusinessRuleException(
                "No se pudo generar el documento: el tipo de acción no tiene una plantilla publicada asignada. Contacte a un administrador para configurarla.");

        if (!string.Equals(action.Status, "GENERADO", StringComparison.OrdinalIgnoreCase))
            await TransitionStatusAsync(actionId, action.Status, "GENERADO",
                "Documento generado.", generatedBy, ct);

        return new CreatePersonnelActionResponse(
            ActionId: actionId, ActionNumber: action.ActionNumber,
            Status: "GENERADO",
            GeneratedDocumentId: docResponse.DocumentId,
            PdfBase64: docResponse.PdfBase64,
            FileName: docResponse.FileName);
    }

    /// <inheritdoc />
    public async Task MarkPendingSignaturesAsync(
        int actionId,
        string? comment,
        int updatedBy,
        CancellationToken ct = default)
    {
        var action = await _actionRepository.GetByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

        if (!action.GeneratedDocumentId.HasValue)
            throw new InvalidOperationException(
                "La acción no tiene un documento generado. Genere el documento antes de marcarlo como pendiente de firmas.");

        await TransitionStatusAsync(actionId, action.Status, "PENDIENTE_FIRMAS", comment, updatedBy, ct);
    }

    /// <inheritdoc />
    public async Task UploadSignedDocumentAsync(
        int actionId,
        UploadSignedDocumentRequest request,
        int updatedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var action = await _actionRepository.GetByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

        await TransitionStatusAsync(actionId, action.Status, "FIRMADO_CARGADO",
            request.Comment, updatedBy, ct);

        // La acción ya quedó FIRMADO_CARGADO (uno de los dos estados que justifican
        // registrar el sueldo en SalaryHistory, junto con VIGENTE) — se registra aquí,
        // una sola vez, sin importar si más abajo continúa a VIGENTE o a FINALIZADO.
        await RecordSalaryHistoryForActionAsync(action, "Acción de personal firmada y cargada.", ct);

        await _actionRepository.LinkSignedDocumentAsync(actionId, request.StoredFileId, ct);

        _logger.LogInformation(
            "PersonnelActionService: documento firmado StoredFileId={FileId} vinculado a acción {ActionId}.",
            request.StoredFileId, actionId);

        var actionType = await _personnelActionType.GetByIdAsync(action.ActionTypeId, ct);

        // 2026-08-06: "Ingresar Histórico" — estos 4 efectos representan una acción EN VIVO
        // sobre sistemas externos/otros registros (crear o bloquear cuenta AD, cerrar un
        // régimen laboral, separar un contrato). Un registro histórico solo documenta algo
        // que ya pasó; no debe disparar ninguno de estos, aunque el documento y la acción
        // en sí se guardan igual más abajo.
        if (!request.IsHistoricalEntry)
        {
            if (actionType?.RequiresAdUserCreation == true)
                await TriggerActionProvisioningAsync(actionId, updatedBy, ct);

            // Renuncia/Jubilación: cierra ÚNICAMENTE el régimen laboral al que corresponde esta
            // acción (el del contrato si lo hay, o el del nombramiento si no) — nunca todos los
            // regímenes activos del empleado, porque puede tener más de uno simultáneo (ej.
            // nombramiento LOSEP + contrato ocasional LOES) y esta separación solo afecta a uno.
            // Se ejecuta ANTES del chequeo de bloqueo de cuenta de abajo, para que ese chequeo
            // vea ya el estado actualizado de los regímenes activos restantes.
            if (actionType?.Code == ResignationRetirementActionTypeCode)
                await CloseLaborRegimeForSeparationAsync(action, updatedBy, ct);

            // Renuncia/Jubilación: si la acción tiene un contrato asociado, cerrarlo a RENUNCIA.
            // Comparación por Code (no un flag de catálogo nuevo) a propósito — evita un cambio
            // de esquema no solicitado; ver PersonnelActionType.RequiresAdUserDisable para el
            // efecto de bloqueo de cuenta, que es universal para este tipo y no depende de esto.
            if (actionType?.Code == ResignationRetirementActionTypeCode && action.ContractId.HasValue)
                await TriggerContractSeparationAsync(actionId, action.ContractId.Value, updatedBy, ct);

            // Va después del cierre de régimen de arriba: el chequeo de "otro régimen activo"
            // dentro de TriggerActionDisableAsync debe ver el estado ya actualizado.
            if (actionType?.RequiresAdUserDisable == true)
                await TriggerActionDisableAsync(actionId, updatedBy, ct);
        }
        else
        {
            _logger.LogInformation(
                "PersonnelActionService: acción {ActionId} cargada como histórica — se omiten aprovisionamiento/bloqueo AD y cierre de régimen por separación.",
                actionId);
        }

        // 2026-07-06: tipos con ReachesVigente=1 (Nombramiento, Traslado, Encargo,
        // Cambio de Sueldo, Asistencia/Horario) pasan automáticamente a VIGENTE al
        // cargar el documento firmado — no requieren el paso manual de "Finalizar".
        // VIGENTE es la fuente de verdad de la que se lee el sueldo/departamento/
        // horario actual del empleado (ver HR.fn_ResolveEmployeeRate).
        if (actionType?.ReachesVigente == true)
        {
            // 2026-08-05: protección para "Ingresar Histórico" — VIGENTE es la fuente de
            // verdad que otras partes del sistema leen como estado ACTUAL del empleado
            // (salario/departamento/horario, ver HR.fn_ResolveEmployeeRate). Si esta acción
            // no es la más reciente por fecha efectiva entre todas las del empleado, no debe
            // pasar por VIGENTE — eso pisaría el estado vigente real (vía
            // CloseSupersededVigenteActionAsync, que cierra CUALQUIER otra VIGENTE sin mirar
            // fechas) y sincronizaría datos viejos a Employee (vía
            // RegisterMovementAndRegimeFromActionAsync). En su lugar, se cierra directo a
            // FINALIZADO — transición ya válida en el grafo de estados — como un registro
            // histórico completo que no reemplaza nada del estado actual.
            var effectiveFrom = action.EffectiveDate ?? action.ActionDate;
            var isMostRecentForEmployee = await IsMostRecentActionForEmployeeAsync(
                actionId, action.EmployeeId ?? 0, effectiveFrom, ct);

            // 2026-08-06: además de "es la más reciente", una acción con fecha fin ya pasada
            // tampoco debe representar el estado VIGENTE actual — ya concluyó. Cubre tanto
            // los históricos con EndDate en el pasado como cualquier acción normal cuyo
            // documento se cargó tarde y para entonces ya venció.
            var alreadyEnded = action.EndDate.HasValue
                && action.EndDate.Value < DateOnly.FromDateTime(DateTime.Today);

            if (isMostRecentForEmployee && !alreadyEnded)
            {
                await TransitionStatusAsync(actionId, "FIRMADO_CARGADO", "VIGENTE",
                    "Vigente automáticamente al cargar el documento firmado.", updatedBy, ct);

                await CloseSupersededVigenteActionAsync(actionId, action.EmployeeId, updatedBy, ct);

                // 2026-07-06: movida aquí desde FinalizeAsync — las acciones con
                // ReachesVigente=1 ya no pasan por FinalizeAsync (van directo y
                // automático a VIGENTE), así que el registro de movimiento/régimen
                // debe dispararse en este punto en vez de esperar a FINALIZADO. El
                // método ya es defensivo (no hace nada si faltan
                // DestinationJobId/DestinationDepartmentId), así que es seguro
                // llamarlo para los 5 tipos ReachesVigente=1, no solo MOVEMENT.
                await RegisterMovementAndRegimeFromActionAsync(actionId, updatedBy, ct);
            }
            else
            {
                var reason = alreadyEnded
                    ? "Registro histórico: su fecha de fin ya pasó — no representa el estado vigente actual ni se sincroniza a Employee."
                    : "Registro histórico: no es la acción más reciente del empleado — no reemplaza el estado vigente actual ni se sincroniza a Employee.";

                await TransitionStatusAsync(actionId, "FIRMADO_CARGADO", "FINALIZADO", reason, updatedBy, ct);

                _logger.LogInformation(
                    "PersonnelActionService: acción {ActionId} es histórica (empleado {EmployeeId}, másReciente={IsMostRecent}, yaConcluyó={AlreadyEnded}) — cerrada directo a FINALIZADO sin pasar por VIGENTE.",
                    actionId, action.EmployeeId, isMostRecentForEmployee, alreadyEnded);
            }
        }
    }

    /// <summary>
    /// Determina si <paramref name="actionId"/> es, por fecha efectiva (EffectiveDate o
    /// ActionDate como fallback), el registro más reciente entre todas las acciones de
    /// <paramref name="employeeId"/> — usado para decidir si una acción puede representar el
    /// estado VIGENTE actual del empleado o si es un ingreso histórico que no debe pisarlo.
    /// </summary>
    private async Task<bool> IsMostRecentActionForEmployeeAsync(
        int actionId, int employeeId, DateOnly effectiveFrom, CancellationToken ct)
    {
        if (employeeId == 0) return true;

        var hasNewerAction = await _db.PersonnelActions
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.ActionId != actionId)
            .AnyAsync(a => (a.EffectiveDate ?? a.ActionDate) > effectiveFrom, ct);

        return !hasNewerAction;
    }

    /// <summary>
    /// 2026-07-06: solo puede haber una acción VIGENTE por empleado a la vez, sin
    /// importar el tipo — al llegar una acción nueva a VIGENTE, cierra (pasa a
    /// FINALIZADO) cualquier otra acción de ese mismo empleado que estuviera
    /// VIGENTE en ese momento.
    /// </summary>
    private async Task CloseSupersededVigenteActionAsync(int newActionId, int? employeeId, int updatedBy, CancellationToken ct)
    {
        if (!employeeId.HasValue || employeeId.Value == 0)
            return;

        var previousVigenteId = await _db.PersonnelActions
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId.Value
                     && a.ActionId != newActionId
                     && a.Status == "VIGENTE")
            .Select(a => (int?)a.ActionId)
            .FirstOrDefaultAsync(ct);

        if (previousVigenteId.HasValue)
            await TransitionStatusAsync(previousVigenteId.Value, "VIGENTE", "FINALIZADO",
                $"Cerrada automáticamente al quedar vigente la acción {newActionId}.", updatedBy, ct);
    }

    /// <inheritdoc />
    public async Task FinalizeAsync(
        int actionId,
        string? comment,
        int updatedBy,
        CancellationToken ct = default)
    {
        var action = await _actionRepository.GetByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

        if (!action.SignedDocumentStoredFileId.HasValue)
            throw new InvalidOperationException(
                "No se puede finalizar sin haber cargado el documento firmado.");

        await TransitionStatusAsync(actionId, action.Status, "FINALIZADO", comment, updatedBy, ct);

        // 2026-07-06: el registro de movimiento/régimen para tipos ReachesVigente=1
        // (antes solo MOVEMENT) se movió a UploadSignedDocumentAsync, porque esos
        // tipos ya no llegan aquí manualmente — pasan directo y automático a VIGENTE.
        // Este método FinalizeAsync ahora solo lo alcanzan tipos con
        // ReachesVigente=0 (Comisión, Licencia, Sanción, Vulnerabilidad, Vacaciones),
        // que no representan cambio de puesto/departamento.
    }

    /// <inheritdoc />
    public async Task CancelAsync(
        int actionId,
        CancelPersonnelActionRequest request,
        int updatedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Se requiere motivo de anulación.");

        var action = await _actionRepository.GetByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

        if (string.Equals(action.Status, "FINALIZADO", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No se puede anular una acción FINALIZADA.");

        await TransitionStatusAsync(actionId, action.Status, "ANULADO", request.Reason, updatedBy, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonnelActionStatusHistoryDto>> GetStatusHistoryAsync(
        int actionId,
        CancellationToken ct = default)
        => await _actionRepository.GetStatusHistoryAsync(actionId, ct);

    /// <inheritdoc />
    public async Task<(string PdfBase64, string FileName)> PreviewDocumentAsync(
        int employeeId,
        Dictionary<string, string> overrides,
        CancellationToken ct = default)
    {
        var templates = await _templateRepository.GetAllAsync(
            templateType: "ACCION_PERSONAL",
            status: DocumentTemplateStatus.Published,
            ct: ct);

        if (templates.Count == 0)
            throw new InvalidOperationException("No hay plantilla ACCION_PERSONAL publicada para previsualización.");

        _logger.LogInformation(
            "Plantilla ACCION_PERSONAL seleccionada. TemplateId={TemplateId}, Code={Code}, Name={Name}, Type={Type}",
            templates[0].TemplateId,
            templates[0].TemplateCode,
            templates[0].Name,
            templates[0].TemplateType);

        var pdfBase64 = await _documentGenerationService.PreviewAsync(
            templates[0].TemplateId, employeeId, overrides, ct);

        var fileName = $"preview-accion-personal-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        return (pdfBase64, fileName);
    }

    // ── Métodos privados ─────────────────────────────────────────────────────────

    private async Task TransitionStatusAsync(
        int actionId,
        string currentStatus,
        string targetStatus,
        string? comment,
        int changedBy,
        CancellationToken ct)
    {
        if (!AllowedTransitions.TryGetValue(currentStatus, out var allowed)
            || !allowed.Contains(targetStatus))
        {
            throw new InvalidOperationException(
                $"Transición no permitida: '{currentStatus}' → '{targetStatus}'.");
        }

        await _actionRepository.UpdateStatusAsync(actionId, targetStatus, ct);
        await WriteHistoryAsync(actionId, fromStatus: currentStatus, toStatus: targetStatus, comment, changedBy, ct);

        _logger.LogInformation(
            "PersonnelActionService: acción {ActionId} transicionó de '{From}' a '{To}'.",
            actionId, currentStatus, targetStatus);
    }

    private async Task WriteHistoryAsync(
        int actionId,
        string? fromStatus,
        string toStatus,
        string? comment,
        int changedBy,
        CancellationToken ct)
    {
        var entry = new PersonnelActionStatusHistory
        {
            ActionId     = actionId,
            FromStatus   = fromStatus,
            StatusCode   = toStatus,
            StatusTypeId = null,  // El repositorio resuelve desde ref_Types si está sembrado
            Comment      = comment,
            ChangedBy    = changedBy,
            ChangedAt    = DateTime.UtcNow
        };

        await _actionRepository.AddStatusHistoryAsync(entry, ct);
    }

    private async Task<GenerateDocumentResponse?> GenerateDocumentForActionAsync(
        int actionId,
        int? employeeId,
        int personId,
        int? contractId,
        Dictionary<string, string>? overrides,
        int generatedBy,
        CancellationToken ct)
    {
        
        var personalAction = await _actionRepository.GetByIdAsync(actionId, ct);
        var personnelActionType = await _personnelActionType.GetByIdAsync(personalAction.ActionTypeId, ct);

        // Si la acción ya tiene un documento generado, reutilizar exactamente la misma versión
        // de plantilla usada originalmente (aunque ahora esté Archived), para no alterar el
        // contenido legal de un documento ya emitido al publicarse una nueva versión.
        int? templateId = personalAction.GeneratedDocumentId.HasValue
            ? await _db.Set<GeneratedDocument>()
                .AsNoTracking()
                .Where(d => d.DocumentId == personalAction.GeneratedDocumentId.Value)
                .Select(d => (int?)d.TemplateId)
                .FirstOrDefaultAsync(ct)
            : null;

        if (templateId is null)
        {
            if (!personnelActionType.DefaultTemplateId.HasValue)
            {
                _logger.LogWarning(
                    "PersonnelActionService: el tipo de acción {ActionTypeId} no tiene plantilla asignada (acción {ActionId}).",
                    personalAction.ActionTypeId, actionId);
                return null;
            }

            var template = await _templateRepository.GetByIdAsync(personnelActionType.DefaultTemplateId.Value, ct);
            if (template is null || template.Status != DocumentTemplateStatus.Published)
            {
                _logger.LogWarning(
                    "PersonnelActionService: la plantilla {TemplateId} del tipo de acción {ActionTypeId} no está publicada (acción {ActionId}).",
                    personnelActionType.DefaultTemplateId, personalAction.ActionTypeId, actionId);
                return null;
            }

            templateId = template.TemplateId;
        }

        // Resolver campos del formulario desde datos reales de la acción
        var detail = await _actionRepository.GetDetailByIdAsync(actionId, ct);
        var actionOverrides = detail is not null
            ? BuildActionOverrides(detail)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "PersonnelActionService: overrides construidos para acción {ActionId} ({Count} keys): [{Keys}]",
            actionId, actionOverrides.Count,
            string.Join(", ", actionOverrides.Select(kv => $"{kv.Key}={kv.Value}")));

        // Los overrides manuales del usuario tienen prioridad sobre los auto-generados
        if (overrides is not null)
            foreach (var kv in overrides)
                actionOverrides[kv.Key] = kv.Value;

        try
        {
            var generateRequest = new GenerateDocumentRequest(
                TemplateId:      templateId.Value,
                EmployeeId:      employeeId is > 0 ? employeeId : null,
                EntityType:      DocumentEntityType.PersonnelAction,
                EntityId:        contractId,
                DocumentNumber:  null,
                Notes:           $"Generado para acción de personal {actionId}",
                ManualOverrides: actionOverrides,
                PersonId:        personId);

            var docResponse = await _documentGenerationService.GenerateAsync(
                generateRequest, generatedBy, ct);

            await _actionRepository.LinkDocumentAsync(actionId, docResponse.DocumentId, ct);

            _logger.LogInformation(
                "PersonnelActionService: documento {DocId} vinculado a acción {ActionId}.",
                docResponse.DocumentId, actionId);

            return docResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PersonnelActionService: error al generar documento para acción {ActionId}.", actionId);
            return null;
        }
    }

    private static Dictionary<string, string> BuildActionOverrides(PersonnelActionDetailDto d)
    {
        var r = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        Set(r, "DOC_NUMBER",         d.ActionNumber);
        Set(r, "ACTA_NUMBER",        d.ActionNumber);
        Set(r, "ELABORATION_DATE",   d.ActionDate.ToString("dd/MM/yyyy"));
        Set(r, "EFFECTIVE_FROM",     d.EffectiveDate?.ToString("dd/MM/yyyy"));
        Set(r, "EFFECTIVE_TO",       d.EndDate is DateOnly ed && ed != DateOnly.MaxValue
                                         ? ed.ToString("dd/MM/yyyy") : null);
        Set(r, "MOTIVATION_TEXT",    d.Reason);
        Set(r, "CURRENT_ADMIN_UNIT", d.OriginDepartmentName);
        Set(r, "CURRENT_JOB_TITLE",  d.OriginJobTitle);
        Set(r, "CURRENT_SALARY",     d.PreviousRmu?.ToString("N2"));
        Set(r, "CURRENT_BUDGET_CODE",d.OriginBudgetCode);
        Set(r, "PROPOSED_ADMIN_UNIT",d.DestinationDepartmentName);
        Set(r, "PROPOSED_JOB_TITLE", d.DestinationJobTitle);
        Set(r, "PROPOSED_SALARY",    d.NewRmu?.ToString("N2"));
        Set(r, "PROPOSED_BUDGET_CODE",d.DestinationBudgetCode);
        Set(r, "EMPLOYEE_FULLNAME",  d.EmployeeFullName);
        Set(r, "EMPLOYEE_IDCARD",    d.EmployeeIdCard);

        // Clasificación de la acción
        // SwornDeclaration=true → presentó la declaración (marca en SI); false → marca en NO APLICA.
        r["DECLARACION_JURADA_SI_MARK"]    = d.SwornDeclaration ? "X" : string.Empty;
        r["DECLARACION_JURADA_MARK"]       = d.SwornDeclaration ? string.Empty : "X";

        // 2026-08-18: el campo único "Proceso Institucional"/"Nivel de Gestión" del
        // formulario describe la clasificación del PUESTO DE DESTINO (a dónde se mueve el
        // empleado), no la de origen — antes se imprimía en SITUACIÓN ACTUAL por error.
        // La clasificación ACTUAL se consulta de la Acción de Personal previa de este
        // mismo empleado (lo que quedó como su PROPUESTA la última vez); si no hay acción
        // previa, queda vacía — nunca se inventa/deriva de otra fuente.
        Set(r, "CURRENT_INSTITUTIONAL_PROCESS",  d.PreviousInstitutionalProcessName);
        Set(r, "CURRENT_MANAGEMENT_LEVEL",       d.PreviousManagementLevelName);
        Set(r, "PROPOSED_INSTITUTIONAL_PROCESS", d.InstitutionalProcessName ?? d.DestinationDepartmentTypeName);
        Set(r, "PROPOSED_MANAGEMENT_LEVEL",      d.ManagementLevelName ?? d.DestinationDepartmentTypeDescription);

        // Responsables del documento: nombre completo y cargo
        Set(r, "DTH_DIRECTOR_NAME",        d.DthDirectorName);
        Set(r, "DTH_DIRECTOR_TITLE",       d.DthDirectorTitle);
        Set(r, "AUTHORITY_NAME",           d.AuthorityNominatorName);
        Set(r, "AUTHORITY_TITLE",          d.AuthorityNominatorTitle);
        Set(r, "ELABORATOR_NAME",          d.ElaboratorName);
        Set(r, "ELABORATOR_TITLE",         d.ElaboratorTitle);
        Set(r, "REVIEWER_NAME",            d.ReviewerName);
        Set(r, "REVIEWER_TITLE",           d.ReviewerTitle);
        Set(r, "REGISTRAR_NAME",           d.RegistrarName);
        Set(r, "REGISTRAR_TITLE",          d.RegistrarTitle);

        var cb = MapActionTypeToCheckbox(d.ActionTypeName);
        if (cb is not null) r[cb] = "X";

        // El formulario oficial UTA tiene dos casillas "REINGRESO" en filas distintas;
        // mientras no exista una subclasificación que las diferencie, se marcan ambas juntas.
        if (cb == "CB_REINGRESO") r["CB_REINGRESO2"] = "X";

        // Detalle libre solo cuando corresponde a la casilla marcada (reutiliza Observations).
        if (cb == "CB_OTRO")
        {
            // "Cambio de Dedicación" no tiene casilla propia en el formulario RGLOSEP — se
            // marca OTRO y se imprime el tipo de acción como detalle en vez de las
            // Observaciones libres (que suelen estar vacías o hablar de otra cosa).
            var isDedicacion = d.ActionTypeName?.ToUpperInvariant().Contains("DEDICACI") ?? false;
            Set(r, "ACTION_OTHER_DETAIL", isDedicacion ? d.ActionTypeName : d.Observations);
        }
        if (cb == "CB_ENCARGO") Set(r, "ACTION_ENCARGO_DETAIL", d.Observations);

        return r;

        static void Set(Dictionary<string, string> dict, string key, string? value)
        {
            if (value is not null) dict[key] = value;
        }
    }

    private static string? MapActionTypeToCheckbox(string? actionTypeName)
    {
        if (actionTypeName is null) return null;
        var n = actionTypeName.ToUpperInvariant();

        // Casos específicos que no calzan (o calzarían mal) con las reglas genéricas de
        // abajo — revisados 2026-08-18 contra los 22 tipos reales de HR.tbl_Personnel_Action_Type.
        // Deben ir ANTES de las reglas genéricas para ganarles (ej. "Cambio de Sueldo"
        // contiene "CAMBIO", que si no fuera por esto marcaría CAMBIO ADMINISTRATIVO).
        if (n.Contains("NOMBRAMIENTO"))                        return "CB_INGRESO";        // Nombramiento / Nombramiento Provisional
        if (n.Contains("PROMOCI"))                             return "CB_ASCENSO";        // Promoción en Categoría y Nivel
        if (n.Contains("REINTEGRO"))                           return "CB_RESTITUCION";    // Reintegro al Cargo
        if (n.Contains("RENUNCIA") || n.Contains("JUBILACI"))  return "CB_CESACION";       // Renuncia o Jubilación
        if (n.Contains("HOMOLOGACI"))                          return "CB_REVISION_CLASI"; // Homologación de Puesto
        if (n.Contains("SUELDO"))                              return "CB_INCREMENTO_RMU"; // Cambio de Sueldo
        if (n.Contains("DEDICACI"))                            return "CB_OTRO";           // Cambio de Dedicación: sin casilla propia en el formulario RGLOSEP

        if (n.Contains("INGRESO") && !n.Contains("REIN"))    return "CB_INGRESO";
        if (n.Contains("REINGRESO"))                          return "CB_REINGRESO";
        if (n.Contains("RESTITU"))                            return "CB_RESTITUCION";
        if (n.Contains("ASCENSO"))                            return "CB_ASCENSO";
        if (n.Contains("TRASLADO"))                           return "CB_TRASLADO";
        if (n.Contains("TRASPASO"))                           return "CB_TRASPASO";
        if (n.Contains("CAMBIO"))                             return "CB_CAMBIO_ADMIN";
        if (n.Contains("INTERCAMBIO"))                        return "CB_INTERCAMBIO";
        if (n.Contains("LICENCIA"))                           return "CB_LICENCIA";
        if (n.Contains("COMISI"))                             return "CB_COMISION";
        if (n.Contains("SANCI"))                              return "CB_SANCIONES";
        if (n.Contains("INCREMENTO"))                         return "CB_INCREMENTO_RMU";
        if (n.Contains("RECATEGORI") || n.Contains("REVISI")) return "CB_REVISION_CLASI";
        if (n.Contains("SUBROGACI"))                          return "CB_SUBROGACION";
        if (n.Contains("ENCARGO"))                            return "CB_ENCARGO";
        if (n.Contains("CESACI"))                             return "CB_CESACION";
        if (n.Contains("DESTITUCI"))                          return "CB_DESTITUCION";
        if (n.Contains("VACACI"))                             return "CB_VACACIONES";
        return "CB_OTRO";
    }

    // ── Aprovisionamiento AD desde Acción de Personal ────────────────────────────

    // Fase 1/4: 2002=CreatedInLocalAd … 2006=LicenseFailed → cuenta AD creada; 2007=LocalAdFailed → sin cuenta
    private static bool ProvisionedAdAccount(int statusId) => statusId is >= 2002 and <= 2006;

    /// <summary>
    /// Dispara el aprovisionamiento de cuenta institucional para el empleado
    /// vinculado a la acción de personal. Delega la lógica al
    /// <see cref="IEmployeeProvisioningOrchestrator"/>.
    /// </summary>
    private async Task TriggerActionProvisioningAsync(int actionId, int updatedBy, CancellationToken ct)
    {
        try
        {
            var action = await _db.PersonnelActions
                .AsNoTracking()
                .Where(a => a.ActionId == actionId)
                .Select(a => new
                {
                    a.PersonId,
                    a.EmployeeId,
                    a.EmployeeTypeId,
                    a.DestinationDepartmentId,
                    a.OriginDepartmentId,
                    a.DestinationJobId,
                    a.OriginJobId,
                    a.GeneratedDocumentId,
                    a.ContractId,
                    a.ActionTypeId,
                    a.ActionNumber,
                    a.EffectiveDate,
                    a.ActionDate
                })
                .FirstOrDefaultAsync(ct);

            if (action is null || action.PersonId == 0)
            {
                _logger.LogWarning(
                    "[ACTION] Aprovisionamiento omitido: PersonId no disponible en ActionId={ActionId}",
                    actionId);
                return;
            }

            var deptId   = action.DestinationDepartmentId ?? action.OriginDepartmentId;
            var jobId    = action.DestinationJobId        ?? action.OriginJobId;
            var empType  = 0;

            if (action.EmployeeId > 0)
            {
                // Empleado existente: leer régimen desde tbl_Employees
                empType = await _db.Employees
                    .AsNoTracking()
                    .Where(e => e.EmployeeId == action.EmployeeId)
                    .Select(e => e.EmployeeType)
                    .FirstOrDefaultAsync(ct) ?? 0;
            }
            else
            {
                // Nuevo ingreso: usar EmployeeTypeId capturado en la acción (seleccionado por el usuario)
                if (action.EmployeeTypeId is > 0)
                {
                    empType = action.EmployeeTypeId.Value;
                }
                else
                {
                    _logger.LogWarning(
                        "[ACTION] Aprovisionamiento omitido: nuevo ingreso sin EmployeeTypeId en ActionId={ActionId} PersonId={PersonId}. " +
                        "Selecciona el Régimen Laboral en el formulario de acción de personal.",
                        actionId, action.PersonId);
                    return;
                }
            }

            string? deptName = deptId.HasValue
                ? await _db.Departments.AsNoTracking()
                    .Where(d => d.DepartmentId == deptId.Value)
                    .Select(d => d.Name)
                    .FirstOrDefaultAsync(ct)
                : null;

            var token = _httpContext.HttpContext?.Request.Headers["Authorization"].FirstOrDefault()
                ?? string.Empty;

            var request = new ProvisioningOrchestrationRequest(
                PersonId:        action.PersonId,
                EmployeeType:    empType,
                DepartmentId:    deptId,
                DepartmentName:  deptName,
                HireDate:        null,
                JobId:           jobId,
                UpdatedBy:       updatedBy,
                BearerToken:     token,
                SourceReference: $"PersonnelAction:{actionId}",
                Source:          ProvisioningSource.PersonnelAction
            );

            var result = await _provisioningOrchestrator.ExecuteAsync(request, ct);

            if (!result.Success && !result.AlreadyExists)
            {
                _logger.LogWarning(
                    "[ACTION] Aprovisionamiento no exitoso. ActionId={ActionId}: {Error}",
                    actionId, result.ErrorMessage);
                return;
            }

            // Si el empleado no existía aún, actualizar el EmployeeId en la acción y en el documento generado
            if ((action.EmployeeId ?? 0) == 0 && result.EmployeeId.HasValue)
            {
                await _db.PersonnelActions
                    .Where(a => a.ActionId == actionId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.EmployeeId, result.EmployeeId.Value),
                        ct);

                // Actualizar también el documento generado si existe
                if (action.GeneratedDocumentId.HasValue)
                {
                    await _db.GeneratedDocuments
                        .Where(d => d.DocumentId == action.GeneratedDocumentId.Value)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(d => d.EmployeeId, result.EmployeeId.Value),
                            ct);
                }

                _logger.LogInformation(
                    "[ACTION] ✓ EmployeeId actualizado. ActionId={ActionId} | EmployeeId={EmployeeId}",
                    actionId, result.EmployeeId.Value);
            }
            // El régimen inicial y el movimiento de INGRESO para el empleado nuevo
            // ya quedan registrados dentro de EmployeeProvisioningOrchestrator.EnsureEmployeeAsync.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ACTION] ERROR en aprovisionamiento. ActionId={ActionId}", actionId);
        }
    }

    /// <summary>
    /// Registra el movimiento de personal (y el régimen laboral, si la acción lo establece)
    /// para acciones de categoría MOVEMENT (Traslado, Encargo) sobre empleados ya existentes.
    /// Se invoca desde <see cref="FinalizeAsync"/>, cuando la acción queda FINALIZADA
    /// (estado terminal — ya no puede anularse). No bloquea la finalización si falla.
    /// </summary>
    private async Task RegisterMovementAndRegimeFromActionAsync(int actionId, int updatedBy, CancellationToken ct)
    {
        var action = await _db.PersonnelActions
            .AsNoTracking()
            .Where(a => a.ActionId == actionId)
            .Select(a => new
            {
                a.EmployeeId,
                a.EmployeeTypeId,
                a.ContractId,
                a.DestinationDepartmentId,
                a.OriginDepartmentId,
                a.DestinationJobId,
                a.ManagementLevel,
                a.ActionTypeId,
                a.ActionNumber,
                a.EffectiveDate,
                a.ActionDate,
            })
            .FirstOrDefaultAsync(ct);

        if (action is null || (action.EmployeeId ?? 0) == 0)
        {
            _logger.LogWarning(
                "[MOVEMENT] Registro omitido: sin EmployeeId para ActionId={ActionId}.", actionId);
            return;
        }

        var effectiveFrom = action.EffectiveDate ?? action.ActionDate;
        var actionType = await _personnelActionType.GetByIdAsync(action.ActionTypeId, ct);

        try
        {
            if (action.DestinationJobId.HasValue && action.DestinationDepartmentId.HasValue)
            {
                var movementTypeId = actionType?.Code is null
                    ? null
                    : await _db.RefTypes
                        .AsNoTracking()
                        .Where(r => r.Category == "MOVEMENT_TYPE" && r.Name == actionType.Code && r.IsActive)
                        .Select(r => (int?)r.TypeId)
                        .FirstOrDefaultAsync(ct);

                await _movementsService.CreateAsync(new PersonnelMovements
                {
                    EmployeeId = action.EmployeeId!.Value,
                    ContractId = action.ContractId,
                    JobId = action.DestinationJobId.Value,
                    OriginDepartmentId = action.OriginDepartmentId,
                    DestinationDepartmentId = action.DestinationDepartmentId.Value,
                    MovementDate = effectiveFrom,
                    MovementTypeId = movementTypeId,
                    PersonnelActionId = actionId,
                    IsActive = true,
                    CreatedBy = updatedBy,
                    CreatedAt = DateTime.Now,
                }, ct);
            }
            else
            {
                _logger.LogInformation(
                    "[MOVEMENT] Registro omitido: sin DestinationJobId/DestinationDepartmentId. ActionId={ActionId}.",
                    actionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MOVEMENT] ERROR registrando movimiento para ActionId={ActionId}.", actionId);
        }

        // La partida presupuestaria (BudgetUnitTypeId) reutiliza el catálogo de
        // ManagementLevel (ref_Types Category='AP_NIVEL_GESTION') — no es un campo propio
        // de la acción. Está ligada a un cambio REAL de departamento, no a cualquier acción
        // que traiga un valor — una acción de solo cambio de sueldo, por ejemplo, no debe
        // mover la partida del empleado aunque el formulario la incluya. Por eso se compara
        // contra el DepartmentID actual del empleado antes de replicar, en vez de aplicar
        // el valor de forma incondicional.
        if (action.ManagementLevel.HasValue && action.DestinationDepartmentId.HasValue)
        {
            try
            {
                var employee = await _employeesRepository.GetByIdAsync(action.EmployeeId!.Value, ct);
                if (employee is not null && employee.DepartmentId != action.DestinationDepartmentId.Value)
                {
                    employee.BudgetUnitTypeId = action.ManagementLevel.Value;
                    await _employeesRepository.UpdateAsync(employee.EmployeeId, employee, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BUDGET-UNIT] ERROR replicando partida presupuestaria para ActionId={ActionId}.", actionId);
            }
        }

        if (!action.EmployeeTypeId.HasValue) return;

        try
        {
            await _laborRegimeService.CreateAsync(new DTOs.EmployeeLaborRegime.EmployeeLaborRegimeCreateDto
            {
                EmployeeId = action.EmployeeId!.Value,
                LaborRegimeId = action.EmployeeTypeId.Value,
                DepartmentId = action.DestinationDepartmentId,
                JobId = action.DestinationJobId,
                IsIndefinite = false,
                DocumentType = "PERSONNEL_ACTION",
                DocumentNumber = action.ActionNumber,
                SourcePersonnelActionId = actionId,
                EffectiveFrom = effectiveFrom,
            }, updatedBy, ct);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogInformation(ex,
                "[LABOR-REGIME] Registro omitido para ActionId={ActionId}: {Message}", actionId, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[LABOR-REGIME] ERROR registrando régimen laboral para ActionId={ActionId}.", actionId);
        }
    }

    private async Task TriggerActionDisableAsync(int actionId, int updatedBy, CancellationToken ct)
    {
        try
        {
            var action = await _db.PersonnelActions
                .AsNoTracking()
                .Where(a => a.ActionId == actionId)
                .Select(a => new { a.EmployeeId, a.PersonId, a.Status })
                .FirstOrDefaultAsync(ct);

            if (action is null || (action.EmployeeId ?? 0) == 0)
            {
                _logger.LogWarning(
                    "[ACTION][DISABLE] Deshabilitar omitido: EmployeeId no disponible en ActionId={ActionId}",
                    actionId);
                return;
            }

            // No bloquear la cuenta si el empleado todavía tiene otro régimen laboral activo
            // (ej. renunció al contrato ocasional LOES pero conserva el nombramiento LOSEP) —
            // mismo criterio que ContractExpirationService.ProcessExpiredContractsAsync. Se
            // evalúa después de que CloseLaborRegimeForSeparationAsync ya cerró, si aplicaba,
            // el régimen específico de esta acción.
            var hasActiveRegime = await _db.Set<EmployeeLaborRegime>()
                .AsNoTracking()
                .AnyAsync(r => r.EmployeeId == action.EmployeeId!.Value && r.IsActive, ct);

            if (hasActiveRegime)
            {
                _logger.LogInformation(
                    "[ACTION][DISABLE] Deshabilitar omitido: EmployeeId={EmployeeId} aún tiene otro régimen laboral activo (ActionId={ActionId}).",
                    action.EmployeeId, actionId);
                return;
            }

            var token = _httpContext.HttpContext?.Request.Headers["Authorization"].FirstOrDefault()
                ?? string.Empty;

            var result = await _provisioningClient.DisableAsync(action.EmployeeId!.Value, token, ct);

            var historyComment = result?.Success == true
                ? $"Cuenta institucional deshabilitada para EmployeeId={action.EmployeeId}."
                : $"Error al deshabilitar cuenta para EmployeeId={action.EmployeeId}: {result?.ErrorMessage ?? "sin respuesta de RepositoryUta"}";

            await WriteHistoryAsync(actionId, action.Status, action.Status, historyComment, updatedBy, ct);

            if (result?.Success == true)
                _logger.LogInformation(
                    "[ACTION][DISABLE] ✓ Cuenta deshabilitada. ActionId={ActionId} | EmployeeId={EmployeeId} | Email={Email}",
                    actionId, action.EmployeeId, result.Email);
            else
                _logger.LogWarning(
                    "[ACTION][DISABLE] ✗ Fallo al deshabilitar. ActionId={ActionId} | EmployeeId={EmployeeId}: {Error}",
                    actionId, action.EmployeeId, result?.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ACTION][DISABLE] ERROR al deshabilitar cuenta. ActionId={ActionId}", actionId);
        }
    }

    /// <summary>
    /// Cierra únicamente el régimen laboral activo al que corresponde esta acción de
    /// renuncia/jubilación — nunca los demás regímenes activos que el empleado pudiera tener
    /// simultáneamente (ej. nombramiento LOSEP + contrato ocasional LOES). Si la acción tiene
    /// contrato asociado, cierra el régimen originado por ese contrato (SourceContractId); si
    /// no (nombramiento), cierra el único régimen activo originado por acción de personal
    /// (DocumentType='PERSONNEL_ACTION'). Defensivo: un fallo o ambigüedad aquí no debe impedir
    /// que el documento firmado quede cargado — en ese caso queda para cierre manual vía
    /// employee-labor-regimes/{id}/close.
    /// </summary>
    private async Task CloseLaborRegimeForSeparationAsync(PersonnelAction action, int updatedBy, CancellationToken ct)
    {
        try
        {
            if (!action.EmployeeId.HasValue) return;

            int regimeId;

            if (action.ContractId.HasValue)
            {
                regimeId = await _db.Set<EmployeeLaborRegime>()
                    .AsNoTracking()
                    .Where(r => r.SourceContractId == action.ContractId.Value && r.IsActive)
                    .Select(r => r.Id)
                    .FirstOrDefaultAsync(ct);
            }
            else
            {
                var candidates = await _db.Set<EmployeeLaborRegime>()
                    .AsNoTracking()
                    .Where(r => r.EmployeeId == action.EmployeeId.Value
                             && r.DocumentType == "PERSONNEL_ACTION"
                             && r.IsActive)
                    .Select(r => r.Id)
                    .ToListAsync(ct);

                if (candidates.Count != 1)
                {
                    _logger.LogWarning(
                        "[ACTION][SEPARATION] No se pudo determinar un único régimen por acción de personal para EmployeeId={EmployeeId} (ActionId={ActionId}, candidatos={Count}). Cierre manual requerido.",
                        action.EmployeeId, action.ActionId, candidates.Count);
                    return;
                }

                regimeId = candidates[0];
            }

            if (regimeId == 0)
            {
                _logger.LogWarning(
                    "[ACTION][SEPARATION] No se encontró régimen laboral activo para cerrar. ActionId={ActionId} EmployeeId={EmployeeId} ContractId={ContractId}.",
                    action.ActionId, action.EmployeeId, action.ContractId);
                return;
            }

            var effectiveTo = action.EffectiveDate ?? DateOnly.FromDateTime(DateTime.Today);

            await _laborRegimeService.CloseAsync(
                regimeId,
                new DTOs.EmployeeLaborRegime.EmployeeLaborRegimeCloseDto { EffectiveTo = effectiveTo },
                updatedBy,
                ct);

            _logger.LogInformation(
                "[ACTION][SEPARATION] ✓ Régimen laboral {RegimeId} cerrado por renuncia/jubilación. ActionId={ActionId} EmployeeId={EmployeeId}.",
                regimeId, action.ActionId, action.EmployeeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ACTION][SEPARATION] ERROR cerrando régimen laboral para ActionId={ActionId}.", action.ActionId);
        }
    }

    /// <summary>
    /// Cierra el contrato asociado a una acción de renuncia/jubilación, transicionándolo a
    /// RENUNCIA (catálogo HR.ref_Types, categoría CONTRACT_STATUS) vía
    /// <see cref="IContractsService.ChangeStatusAsync"/> — respeta la máquina de estados real
    /// (HR.tbl_contract_status_transitions), no escribe el campo directamente. Defensivo: un
    /// fallo aquí no debe impedir que el documento firmado quede cargado ni que la cuenta se
    /// haya deshabilitado (mismo criterio que <see cref="TriggerActionDisableAsync"/>).
    /// </summary>
    private async Task TriggerContractSeparationAsync(int actionId, int contractId, int updatedBy, CancellationToken ct)
    {
        try
        {
            var renunciaTypeId = await _db.RefTypes
                .AsNoTracking()
                .Where(r => r.Category == "CONTRACT_STATUS" && r.Name == "RENUNCIA")
                .Select(r => (int?)r.TypeId)
                .FirstOrDefaultAsync(ct);

            if (!renunciaTypeId.HasValue)
            {
                _logger.LogWarning(
                    "[ACTION][SEPARATION] No existe HR.ref_Types CONTRACT_STATUS='RENUNCIA'. ActionId={ActionId} ContractId={ContractId}",
                    actionId, contractId);
                return;
            }

            await _contractsService.ChangeStatusAsync(
                contractId, renunciaTypeId.Value,
                $"Cerrado automáticamente por renuncia/jubilación (acción de personal {actionId}).", ct);

            _logger.LogInformation(
                "[ACTION][SEPARATION] ✓ Contrato {ContractId} transicionado a RENUNCIA por acción {ActionId}.",
                contractId, actionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ACTION][SEPARATION] ERROR al cerrar contrato {ContractId} para ActionId={ActionId}.",
                contractId, actionId);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonnelActionReportDto>> GetForReportAsync(ReportFilterDto filter, CancellationToken ct = default)
    {
        var start = filter.StartDate.HasValue ? DateOnly.FromDateTime(filter.StartDate.Value) : (DateOnly?)null;
        var end   = filter.EndDate.HasValue   ? DateOnly.FromDateTime(filter.EndDate.Value)   : (DateOnly?)null;
        var categories = filter.ActionCategories?.ToList();

        var query =
            from a in _db.PersonnelActions.AsNoTracking()
            join p   in _db.People.AsNoTracking()                on a.PersonId       equals p.PersonId
            join pat in _db.PersonnelActionTypes.AsNoTracking()  on a.ActionTypeId   equals pat.PersonnelActionTypeId
            join od  in _db.Departments.AsNoTracking()           on a.OriginDepartmentId      equals od.DepartmentId  into odg
            from od in odg.DefaultIfEmpty()
            join dd  in _db.Departments.AsNoTracking()           on a.DestinationDepartmentId equals dd.DepartmentId  into ddg
            from dd in ddg.DefaultIfEmpty()
            where (!start.HasValue || a.ActionDate >= start.Value)
               && (!end.HasValue   || a.ActionDate <= end.Value)
               && (string.IsNullOrEmpty(filter.Status) || a.Status == filter.Status)
               && (!filter.EmployeeId.HasValue  || a.EmployeeId == filter.EmployeeId.Value || a.PersonId == filter.EmployeeId.Value)
               && (!filter.ActionTypeId.HasValue || a.ActionTypeId == filter.ActionTypeId.Value)
               && (categories == null || categories.Count == 0 || categories.Contains(pat.ActionCategory))
            orderby p.LastName, p.FirstName, a.ActionDate descending
            select new PersonnelActionReportDto
            {
                ActionId               = a.ActionId,
                ActionNumber           = a.ActionNumber,
                PersonIdCard           = p.IdCard,
                PersonFullName         = p.LastName + " " + p.FirstName,
                DepartmentName         = od != null ? od.Name : dd != null ? dd.Name : null,
                ActionTypeName         = pat.Name,
                ActionCategory         = pat.ActionCategory,
                ActionDate             = a.ActionDate.ToDateTime(TimeOnly.MinValue),
                EffectiveDate          = a.EffectiveDate.HasValue ? a.EffectiveDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                EndDate                = a.EndDate.HasValue ? a.EndDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                InstitutionalProcessName = null,
                StatusName             = a.Status,
                HasDocument            = a.GeneratedDocumentId.HasValue
            };

        return await query.ToListAsync(ct);
    }
}

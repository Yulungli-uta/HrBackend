using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.DTOs.PersonnelActions;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Application.Interfaces.Services.Documents;
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
            ["FIRMADO_CARGADO"]  = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FINALIZADO", "ANULADO" },
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
    private readonly ILogger<PersonnelActionService> _logger;

    public PersonnelActionService(
        IPersonnelActionRepository actionRepository,
        IPersonnelActionTypeRepository personnelActionType,
        IEmployeesRepository employeesRepository,
        IDocumentTemplateRepository templateRepository,
        IDocumentGenerationService documentGenerationService,
        ILogger<PersonnelActionService> logger)
    {
        _actionRepository          = actionRepository;
        _personnelActionType       = personnelActionType;
        _employeesRepository       = employeesRepository;
        _templateRepository        = templateRepository;
        _documentGenerationService = documentGenerationService;
        _logger                    = logger;
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

        int personId = request.personId;
        var employee = await _employeesRepository.GetByPersonIdAsync(personId, ct);

        var action = new PersonnelAction
        {
            EmployeeId              = request.EmployeeId,
            ActionTypeId            = request.ActionTypeId,
            ActionNumber            = request.ActionNumber?.Trim(),
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
                ActionId: actionId, ActionNumber: request.ActionNumber,
                Status: "BORRADOR", GeneratedDocumentId: null,
                PdfBase64: null, FileName: null);
        }

        var docResponse = await GenerateDocumentForActionAsync(
            actionId, request.EmployeeId, request.ContractId,
            request.DocumentOverrides, createdBy, ct);

        return new CreatePersonnelActionResponse(
            ActionId: actionId, ActionNumber: request.ActionNumber,
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
        action.DthDirectorId           = request.DthDirectorId;
        action.AuthorityNominatorId    = request.AuthorityNominatorId;
        action.ElaboratorId            = request.ElaboratorId;
        action.ReviewerId              = request.ReviewerId;
        action.RegistrarId             = request.RegistrarId;
        action.UpdatedAt               = DateTime.UtcNow;
        action.UpdatedBy               = updatedBy;

        await _actionRepository.UpdateAsync(action, ct);
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

        GenerateDocumentResponse? docResponse = null;

        if (request.GenerateDocumentIfMissing && !action.GeneratedDocumentId.HasValue)
        {
            docResponse = await GenerateDocumentForActionAsync(
                actionId, action.EmployeeId, action.ContractId,
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

        var terminalStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "FINALIZADO", "ANULADO", "CANCELLED" };

        if (terminalStatuses.Contains(action.Status))
            throw new InvalidOperationException(
                $"No se puede generar documento en estado '{action.Status}'.");

        var docResponse = await GenerateDocumentForActionAsync(
            actionId, action.EmployeeId, action.ContractId,
            overrides, generatedBy, ct);

        if (docResponse is not null &&
            !string.Equals(action.Status, "GENERADO", StringComparison.OrdinalIgnoreCase))
            await TransitionStatusAsync(actionId, action.Status, "GENERADO",
                "Documento generado.", generatedBy, ct);

        return new CreatePersonnelActionResponse(
            ActionId: actionId, ActionNumber: action.ActionNumber,
            Status: docResponse is not null ? "GENERADO" : action.Status,
            GeneratedDocumentId: docResponse?.DocumentId,
            PdfBase64: docResponse?.PdfBase64,
            FileName: docResponse?.FileName);
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

        await _actionRepository.LinkSignedDocumentAsync(actionId, request.StoredFileId, ct);

        _logger.LogInformation(
            "PersonnelActionService: documento firmado StoredFileId={FileId} vinculado a acción {ActionId}.",
            request.StoredFileId, actionId);
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

        _logger.LogInformation(
            "Plantilla ACCION_PERSONAL seleccionada. TemplateId={TemplateId}, Code={Code}, Name={Name}, Type={Type}",
            templates[0].TemplateId,
            templates[0].TemplateCode,
            templates[0].Name,
            templates[0].TemplateType);

        if (templates.Count == 0)
            throw new InvalidOperationException("No hay plantilla ACCION_PERSONAL publicada para previsualización.");

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
        int employeeId,
        int? contractId,
        Dictionary<string, string>? overrides,
        int generatedBy,
        CancellationToken ct)
    {
        
        var personalAction = await _actionRepository.GetByIdAsync(actionId, ct);
        _logger.LogInformation($"*****************Actionid: {actionId}, ActionTypeId: {personalAction.ActionTypeId} ");

        var personnelActionType = await _personnelActionType.GetByIdAsync(personalAction.ActionTypeId, ct);

        //var templates = await _templateRepository.GetByIdAsync(personnelActionType.TemplateCode, ct);

        //_logger.LogInformation($"********************* templates: {templates.Name}, description: {templates.Description}");

        var templates = await _templateRepository.GetAllAsync(
            //templateType: "ACCION_PERSONAL",
            templateType: personnelActionType.TemplateCode,
            status: DocumentTemplateStatus.Published,
            ct: ct);


        if (templates.Count == 0)
        {
            _logger.LogWarning(
                "PersonnelActionService: sin plantilla publicada ACCION_PERSONAL para acción {ActionId}.",
                actionId);
            return null;
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
                TemplateId:      templates[0].TemplateId,                
                EmployeeId:      employeeId,
                EntityType:      DocumentEntityType.PersonnelAction,
                EntityId:        contractId,   // null → resolver usa contrato activo más reciente
                DocumentNumber:  null,
                Notes:           $"Generado para acción de personal {actionId}",
                ManualOverrides: actionOverrides);

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
        r["DECLARACION_JURADA_MARK"]       = d.SwornDeclaration ? "X" : string.Empty;
        Set(r, "CURRENT_INSTITUTIONAL_PROCESS", d.InstitutionalProcessName);
        Set(r, "CURRENT_MANAGEMENT_LEVEL",      d.ManagementLevelName);

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
}

using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.DTOs.Documents.PersonnelActions;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services.Documents;

/// <summary>
/// Servicio de gestión de acciones de personal (LOSEP / RLOSEP).
/// Coordina la creación de la acción con la generación automática del documento PDF
/// cuando el solicitante lo requiere.
/// </summary>
public sealed class PersonnelActionService : IPersonnelActionService
{
    private readonly IPersonnelActionRepository _actionRepository;
    private readonly IDocumentTemplateRepository _templateRepository;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<PersonnelActionService> _logger;

    public PersonnelActionService(
        IPersonnelActionRepository actionRepository,
        IDocumentTemplateRepository templateRepository,
        IDocumentGenerationService documentGenerationService,
        ILogger<PersonnelActionService> logger)
    {
        _actionRepository          = actionRepository;
        _templateRepository        = templateRepository;
        _documentGenerationService = documentGenerationService;
        _logger                    = logger;
    }

    /// <inheritdoc />
    public async Task<PagedPersonnelActionResult> GetPagedAsync(
        PersonnelActionQueryFilter filter,
        CancellationToken ct = default)
    {
        return await _actionRepository.GetPagedAsync(filter, ct);
    }

    /// <inheritdoc />
    public async Task<PersonnelActionDetailDto> GetDetailByIdAsync(
        int actionId,
        CancellationToken ct = default)
    {
        return await _actionRepository.GetDetailByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonnelActionSummaryDto>> GetByEmployeeIdAsync(
        int employeeId,
        CancellationToken ct = default)
    {
        return await _actionRepository.GetByEmployeeIdAsync(employeeId, ct);
    }

    /// <inheritdoc />
    public async Task<CreatePersonnelActionResponse> CreateAsync(
        CreatePersonnelActionRequest request,
        int createdBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "PersonnelActionService: creando acción de personal para empleado {EmployeeId}, tipo {ActionTypeId}.",
            request.EmployeeId, request.ActionTypeId);

        // ── 1. Crear la acción de personal ───────────────────────────────────────
        var action = new PersonnelAction
        {
            EmployeeId            = request.EmployeeId,
            ActionTypeId          = request.ActionTypeId,
            ActionNumber          = request.ActionNumber?.Trim(),
            ActionDate            = request.ActionDate,
            EffectiveDate         = request.EffectiveDate,
            EndDate               = request.EndDate,
            OriginDepartmentId    = request.OriginDepartmentId,
            OriginJobId           = request.OriginJobId,
            OriginBudgetCode      = request.OriginBudgetCode?.Trim(),
            DestinationDepartmentId = request.DestinationDepartmentId,
            DestinationJobId      = request.DestinationJobId,
            DestinationBudgetCode = request.DestinationBudgetCode?.Trim(),
            PreviousRmu           = request.PreviousRmu,
            NewRmu                = request.NewRmu,
            LegalBasis            = request.LegalBasis?.Trim(),
            Reason                = request.Reason?.Trim(),
            Observations          = request.Observations?.Trim(),
            ContractId            = request.ContractId,
            MovementId            = request.MovementId,
            Status                = "DRAFT",
            CreatedAt             = DateTime.UtcNow,
            CreatedBy             = createdBy
        };

        var actionId = await _actionRepository.CreateAsync(action, ct);

        _logger.LogInformation(
            "PersonnelActionService: acción {ActionId} creada en estado DRAFT.",
            actionId);

        // ── 2. Generar documento PDF si se solicitó ──────────────────────────────
        if (!request.GenerateDocument)
        {
            return new CreatePersonnelActionResponse(
                ActionId:            actionId,
                ActionNumber:        request.ActionNumber,
                Status:              "DRAFT",
                GeneratedDocumentId: null,
                PdfBase64:           null,
                FileName:            null);
        }

        var docResponse = await GenerateDocumentForActionAsync(
            actionId,
            request.EmployeeId,
            request.ContractId,
            request.DocumentOverrides,
            createdBy,
            ct);

        return new CreatePersonnelActionResponse(
            ActionId:            actionId,
            ActionNumber:        request.ActionNumber,
            Status:              "DRAFT",
            GeneratedDocumentId: docResponse?.DocumentId,
            PdfBase64:           docResponse?.PdfBase64,
            FileName:            docResponse?.FileName);
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

        if (action.Status is "APPROVED" or "EXECUTED" or "CANCELLED")
            throw new InvalidOperationException(
                $"No se puede modificar una acción en estado '{action.Status}'.");

        action.ActionNumber          = request.ActionNumber?.Trim();
        action.ActionDate            = request.ActionDate;
        action.EffectiveDate         = request.EffectiveDate;
        action.EndDate               = request.EndDate;
        action.OriginDepartmentId    = request.OriginDepartmentId;
        action.OriginJobId           = request.OriginJobId;
        action.OriginBudgetCode      = request.OriginBudgetCode?.Trim();
        action.DestinationDepartmentId = request.DestinationDepartmentId;
        action.DestinationJobId      = request.DestinationJobId;
        action.DestinationBudgetCode = request.DestinationBudgetCode?.Trim();
        action.PreviousRmu           = request.PreviousRmu;
        action.NewRmu                = request.NewRmu;
        action.LegalBasis            = request.LegalBasis?.Trim();
        action.Reason                = request.Reason?.Trim();
        action.Observations          = request.Observations?.Trim();
        action.UpdatedAt             = DateTime.UtcNow;
        action.UpdatedBy             = updatedBy;

        await _actionRepository.UpdateAsync(action, ct);

        _logger.LogInformation(
            "PersonnelActionService: acción {ActionId} actualizada por usuario {User}.",
            actionId, updatedBy);
    }

    /// <inheritdoc />
    public async Task<CreatePersonnelActionResponse> ApproveAsync(
        int actionId,
        ApprovePersonnelActionRequest request,
        int approvedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var action = await _actionRepository.GetByIdAsync(actionId, ct)
            ?? throw new KeyNotFoundException($"Acción de personal {actionId} no encontrada.");

        if (action.Status is "APPROVED" or "EXECUTED" or "CANCELLED")
            throw new InvalidOperationException(
                $"La acción ya se encuentra en estado '{action.Status}'.");

        await _actionRepository.UpdateStatusAsync(actionId, "APPROVED", ct);

        _logger.LogInformation(
            "PersonnelActionService: acción {ActionId} aprobada por usuario {User}.",
            actionId, approvedBy);

        // Generar documento si no tiene uno y se solicitó
        if (request.GenerateDocumentIfMissing && !action.GeneratedDocumentId.HasValue)
        {
            var docResponse = await GenerateDocumentForActionAsync(
                actionId,
                action.EmployeeId,
                action.ContractId,
                overrides: null,
                approvedBy,
                ct);

            return new CreatePersonnelActionResponse(
                ActionId:            actionId,
                ActionNumber:        action.ActionNumber,
                Status:              "APPROVED",
                GeneratedDocumentId: docResponse?.DocumentId,
                PdfBase64:           docResponse?.PdfBase64,
                FileName:            docResponse?.FileName);
        }

        return new CreatePersonnelActionResponse(
            ActionId:            actionId,
            ActionNumber:        action.ActionNumber,
            Status:              "APPROVED",
            GeneratedDocumentId: action.GeneratedDocumentId,
            PdfBase64:           null,
            FileName:            null);
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

        var docResponse = await GenerateDocumentForActionAsync(
            actionId,
            action.EmployeeId,
            action.ContractId,
            overrides,
            generatedBy,
            ct);

        return new CreatePersonnelActionResponse(
            ActionId:            actionId,
            ActionNumber:        action.ActionNumber,
            Status:              action.Status,
            GeneratedDocumentId: docResponse?.DocumentId,
            PdfBase64:           docResponse?.PdfBase64,
            FileName:            docResponse?.FileName);
    }

    // ── Métodos privados ─────────────────────────────────────────────────────────

    /// <summary>
    /// Busca la plantilla publicada de tipo ACCION_PERSONAL, genera el PDF
    /// y vincula el documento generado a la acción de personal.
    /// </summary>
    private async Task<GenerateDocumentResponse?> GenerateDocumentForActionAsync(
        int actionId,
        int employeeId,
        int? contractId,
        Dictionary<string, string>? overrides,
        int generatedBy,
        CancellationToken ct)
    {
        // Buscar plantilla publicada de tipo ACCION_PERSONAL
        var templates = await _templateRepository.GetAllAsync(
            templateType: "ACCION_PERSONAL",
            status: DocumentTemplateStatus.Published,
            ct: ct);

        if (templates.Count == 0)
        {
            _logger.LogWarning(
                "PersonnelActionService: no se encontró plantilla publicada de tipo ACCION_PERSONAL. " +
                "El documento no fue generado para la acción {ActionId}.",
                actionId);
            return null;
        }

        // Usar la primera plantilla publicada (la más reciente por convención)
        var templateId = templates[0].TemplateId;

        try
        {
            var generateRequest = new GenerateDocumentRequest(
                TemplateId:      templateId,
                EmployeeId:      employeeId,
                EntityType:      DocumentEntityType.PersonnelAction,
                EntityId:        actionId,
                DocumentNumber:  null,
                Notes:           $"Generado automáticamente para acción de personal {actionId}",
                ManualOverrides: overrides);

            var docResponse = await _documentGenerationService.GenerateAsync(
                generateRequest, generatedBy, ct);

            // Vincular documento a la acción
            await _actionRepository.LinkDocumentAsync(actionId, docResponse.DocumentId, ct);

            _logger.LogInformation(
                "PersonnelActionService: documento {DocId} vinculado a acción {ActionId}.",
                docResponse.DocumentId, actionId);

            return docResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PersonnelActionService: error al generar documento para acción {ActionId}.",
                actionId);
            // No propagamos el error — la acción se creó correctamente aunque falle el PDF
            return null;
        }
    }
}

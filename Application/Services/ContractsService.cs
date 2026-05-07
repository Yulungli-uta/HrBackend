using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Contracts;
using WsUtaSystem.Application.DTOs.ContractStatusHistory;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.Interfaces.Repositories.Documents;
using WsUtaSystem.Application.Interfaces.Services.Documents;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public class ContractsService : Service<Contracts, int>, IContractsService
{

    private const string ContractStatusCategory = "CONTRACT_STATUS";

    private const string StatusBorrador = "BORRADOR";
    private const string StatusGenerado = "GENERADO";
    private const string StatusPendienteFirmas = "PENDIENTE_FIRMAS";
    private const string StatusFirmadoCargado = "FIRMADO_CARGADO";
    private const string StatusFinalizado = "FINALIZADO";
    private const string StatusAnulado = "ANULADO";

    private readonly IContractsRepository _repository;
    private readonly AppDbContext _db;

    private readonly IEmailBuilder _emailBuilder;
    private readonly ICurrentUserService _currentUser;
    private readonly IvwEmployeeDetailsService _employeeDetails;
    private readonly IRefTypesService _refTypes;
    private readonly IDocumentTemplateRepository _templateRepository;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly IContractTypeRepository _contractTypeRepository;
    private readonly ILogger<ContractsService> _logger;

    public ContractsService(
        IContractsRepository repo,
        AppDbContext db,
        IEmailBuilder emailBuilder,
        ICurrentUserService currentUser,
        IvwEmployeeDetailsService employeeDetails,
        IRefTypesService refTypes,
        IDocumentTemplateRepository templateRepository,
        IDocumentGenerationService documentGenerationService,
        IContractTypeRepository contractTypeRepository,
        ILogger<ContractsService> logger
    ) : base(repo)
    {
        _repository                = repo                    ?? throw new ArgumentNullException(nameof(repo));
        _db                        = db                      ?? throw new ArgumentNullException(nameof(db));
        _emailBuilder              = emailBuilder            ?? throw new ArgumentNullException(nameof(emailBuilder));
        _currentUser               = currentUser             ?? throw new ArgumentNullException(nameof(currentUser));
        _employeeDetails           = employeeDetails         ?? throw new ArgumentNullException(nameof(employeeDetails));
        _refTypes                  = refTypes                ?? throw new ArgumentNullException(nameof(refTypes));
        _templateRepository        = templateRepository      ?? throw new ArgumentNullException(nameof(templateRepository));
        _documentGenerationService = documentGenerationService ?? throw new ArgumentNullException(nameof(documentGenerationService));
        _contractTypeRepository    = contractTypeRepository  ?? throw new ArgumentNullException(nameof(contractTypeRepository));
        _logger                    = logger                  ?? throw new ArgumentNullException(nameof(logger));
    }

    // -------------------------------------------------------
    // Compatibilidad con el uso antiguo (entity)
    // -------------------------------------------------------
    public new Task<Contracts> CreateAsync(Contracts entity, CancellationToken ct)
        => CreateAndNotifyAsync(entity, ct);

    public new Task UpdateAsync(int id, Contracts entity, CancellationToken ct)
        => UpdateAndNotifyAsync(id, entity, ct);

    // -------------------------------------------------------
    // NUEVO: Update desde DTO (controller delgado)
    // -------------------------------------------------------
    public async Task UpdateAsync(int id, ContractsUpdateDto dto, CancellationToken ct)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (dto.ContractID != 0 && dto.ContractID != id)
            throw new ArgumentException("ContractID del body no coincide con el id de la ruta.");

        var strategy = _db.Database.CreateExecutionStrategy();
        Contracts? updated = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var current = await _repository.GetByIdAsync(id, ct);
            if (current is null)
                throw new KeyNotFoundException($"Contracts con id={id} no existe.");

            // ✅ Update campo por campo (en Service, buena práctica)
            ApplyDto(dto, current);

            ValidateDates(current);

            // Si manejas RowVersion en tu arquitectura, aquí va el OriginalValue:
            // _db.Entry(current).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

            await base.UpdateAsync(id, current, ct);

            updated = await _repository.GetByIdAsync(id, ct);

            await tx.CommitAsync(ct);
        });

        if (updated is not null)
            await NotifyOnUpdateAsync(updated, ct);
    }

    // -------------------------------------------------------
    // Métodos de negocio existentes (entity)
    // -------------------------------------------------------
    public async Task<Contracts> CreateAndNotifyAsync(Contracts entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        ValidateDates(entity);

        // El estado inicial siempre es BORRADOR independientemente de lo que envíe el cliente
        entity.Status = await GetContractStatusIdAsync(StatusBorrador, ct);

        var isRootContract = entity.ParentID is null;

        // Validar flujo solo para contratos raíz (no adendums)
        if (isRootContract)
            await ValidateCanCreateContractAsync(entity.CertificationID, ct);

        // Auto-numerar si no viene código
        if (string.IsNullOrWhiteSpace(entity.ContractCode))
        {
            var (docNumber, _, _) = await _contractTypeRepository
                .ConsumeNextNumberAsync(entity.ContractTypeID, DateTime.Now.Year, ct);
            entity.ContractCode = docNumber;
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        Contracts? created = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            created = await base.CreateAsync(entity, ct);

            // Descontar cupo solo en contratos raíz
            if (isRootContract && entity.CertificationID.HasValue)
                await IncrementRequestTotalHiredAsync(entity.CertificationID.Value, ct);

            await tx.CommitAsync(ct);
        });

        if (created is not null)
            await NotifyOnCreateAsync(created, ct);

        return created!;
    }

    public async Task ValidateCanCreateContractAsync(int? certificationId, CancellationToken ct = default)
    {
        if (!certificationId.HasValue)
            throw new InvalidOperationException("Se requiere una certificación financiera para crear el contrato.");

        var cert = await _db.Set<FinancialCertification>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CertificationId == certificationId.Value, ct)
            ?? throw new KeyNotFoundException($"Certificación id={certificationId} no existe.");

        // Verificar que la certificación esté aprobada
        var approvedId = await GetContractStatusIdByCategory("FIN_CERT_STATUS", "APROBADA", ct);
        if (cert.Status != approvedId)
            throw new InvalidOperationException(
                "La certificación financiera debe estar aprobada para crear contratos.");

        // Verificar cupo disponible
        if (!cert.RequestId.HasValue)
            throw new InvalidOperationException("La certificación no está asociada a ninguna solicitud de contrato.");

        var request = await _db.Set<ContractRequest>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RequestId == cert.RequestId.Value, ct)
            ?? throw new KeyNotFoundException($"Solicitud id={cert.RequestId} no existe.");

        if (request.PendingCount <= 0)
            throw new InvalidOperationException(
                $"No hay cupo disponible. Solicitados: {request.NumberOfPeopleToHire}, Contratados: {request.TotalPeopleHired}.");
    }

    private async Task IncrementRequestTotalHiredAsync(int certificationId, CancellationToken ct)
    {
        var cert = await _db.Set<FinancialCertification>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CertificationId == certificationId, ct);

        if (cert?.RequestId is null) return;

        var request = await _db.Set<ContractRequest>()
            .FirstOrDefaultAsync(x => x.RequestId == cert.RequestId.Value, ct);

        if (request is null) return;

        request.TotalPeopleHired++;
        request.UpdatedAt = DateTime.Now;

        var newStatusName = request.TotalPeopleHired >= request.NumberOfPeopleToHire
            ? "COMPLETADO"
            : "EN_PROCESO";

        request.Status = await GetContractStatusIdByCategory("CONTRACT_REQUEST_STATUS", newStatusName, ct);

        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> GetContractStatusIdByCategory(string category, string name, CancellationToken ct)
    {
        var statusId = await _db.RefTypes
            .AsNoTracking()
            .Where(x => x.Category == category && x.Name == name && x.IsActive)
            .Select(x => x.TypeId)
            .FirstOrDefaultAsync(ct);

        if (statusId <= 0)
            throw new InvalidOperationException($"Estado '{category}/{name}' no existe en ref_Types.");

        return statusId;
    }

    public async Task UpdateAndNotifyAsync(int id, Contracts entity, CancellationToken ct)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        ValidateDates(entity);

        var strategy = _db.Database.CreateExecutionStrategy();
        Contracts? updated = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var current = await _repository.GetByIdAsync(id, ct);
            if (current is null)
                throw new KeyNotFoundException($"Contracts con id={id} no existe.");

            CopyUpdatableFields(source: entity, target: current);

            await base.UpdateAsync(id, current, ct);

            updated = await _repository.GetByIdAsync(id, ct);

            await tx.CommitAsync(ct);
        });

        if (updated is not null)
            await NotifyOnUpdateAsync(updated, ct);
    }

    public async Task<IReadOnlyList<int>> GetAllowedNextStatusesAsync(int currentStatusTypeId, CancellationToken ct)
    {
        var next = await _db.ContractStatusTransitions
            .AsNoTracking()
            .Where(x => x.IsActive && x.FromStatusTypeID == currentStatusTypeId)
            .Select(x => x.ToStatusTypeID)
            .Distinct()
            .ToListAsync(ct);

        return next;
    }

    public async Task ChangeStatusAsync(int contractId, int toStatusTypeId, string? comment, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var contract = await _db.Set<Contracts>().FirstOrDefaultAsync(x => x.ContractID == contractId, ct);
            if (contract is null) throw new KeyNotFoundException($"Contrato id={contractId} no existe.");

            var fromStatus = contract.Status; // asumiendo int
            if (fromStatus == toStatusTypeId)
                return; // NoOp

            var allowed = await _db.ContractStatusTransitions
                .AsNoTracking()
                .AnyAsync(x => x.IsActive && x.FromStatusTypeID == fromStatus && x.ToStatusTypeID == toStatusTypeId, ct);

            if (!allowed)
                throw new InvalidOperationException($"Transición no permitida: {fromStatus} -> {toStatusTypeId}");

            // Actualiza estado
            contract.Status = toStatusTypeId;

            // Histórico (auditoría por JWT)
            var userId = _currentUser.EmployeeId; // según tu implementación
            _db.ContractStatusHistories.Add(new ContractStatusHistory
            {
                ContractID = contractId,
                StatusTypeID = toStatusTypeId,
                Comment = comment,
                ChangedBy = userId > 0 ? userId : null,
                ChangedAt = DateTime.Now
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    public async Task<IReadOnlyList<ContractStatusHistoryDto>> GetStatusHistoryAsync(int contractId, CancellationToken ct)
    {
        // RefTypes por categoría (no hardcode de IDs)
        var refTypes = await _refTypes.GetByCategoryAsync("CONTRACT_STATUS", ct);
        var map = refTypes.ToDictionary(x => x.TypeId, x => x.Name);

        var items = await _db.ContractStatusHistories
            .AsNoTracking()
            .Where(x => x.ContractID == contractId)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync(ct);

        return items.Select(h => new ContractStatusHistoryDto
        {
            HistoryID = h.HistoryID,
            ContractID = h.ContractID,
            StatusTypeID = h.StatusTypeID,
            StatusName = map.TryGetValue(h.StatusTypeID, out var name) ? name : null,
            Comment = h.Comment,
            ChangedAt = h.ChangedAt,
            ChangedBy = h.ChangedBy
        }).ToList();
    }

    public async Task<IReadOnlyList<Contracts>> GetAddendumsAsync(int contractId, CancellationToken ct)
    {
        return await _db.Set<Contracts>()
            .AsNoTracking()
            .Where(x => x.ParentID == contractId)
            .OrderByDescending(x => x.ContractID)
            .ToListAsync(ct);
    }

    // -------------------------------------------------------
    // Motor documental: freeze / unfreeze / estado de documento
    // -------------------------------------------------------

    public Task FreezeDocumentAsync(int contractId, int documentId, int templateVersion, CancellationToken ct)
        => _repository.FreezeDocumentAsync(contractId, documentId, templateVersion, ct);

    public Task UnfreezeDocumentAsync(int contractId, CancellationToken ct)
        => _repository.UnfreezeDocumentAsync(contractId, ct);

    public async Task<WsUtaSystem.Application.DTOs.Contracts.ContractDocumentStatusDto?> GetDocumentStatusAsync(int contractId, CancellationToken ct)
    {
        var contract = await _repository.GetWithDocumentInfoAsync(contractId, ct);
        if (contract is null) return null;

        string? docStatus = null;
        string? fileName  = null;
        int?    fileId    = null;

        if (contract.GeneratedDocumentId.HasValue)
        {
            var doc = await _db.GeneratedDocuments
                .AsNoTracking()
                .Where(d => d.DocumentId == contract.GeneratedDocumentId.Value)
                .Select(d => new { d.Status, d.FileName, d.StoredFileId })
                .FirstOrDefaultAsync(ct);

            docStatus = doc?.Status;
            fileName  = doc?.FileName;
            fileId    = doc?.StoredFileId;
        }

        return new WsUtaSystem.Application.DTOs.Contracts.ContractDocumentStatusDto(
            contract.ContractID,
            contract.GeneratedDocumentId,
            contract.TemplateVersionUsed,
            contract.IsDocumentFrozen,
            docStatus,
            fileName,
            fileId
        );
    }

    // -------------------------------------------------------
    // Notificaciones
    // -------------------------------------------------------
    private async Task NotifyOnCreateAsync(Contracts created, CancellationToken ct)
    {
        try
        {
            await _currentUser.LoadBossAsync(ct);
            var toBoss = _currentUser.BossEmail?.Trim();

            if (!string.IsNullOrWhiteSpace(toBoss))
            {
                var body = BuildCreateEmailBody(created);
                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Contrato creado: {created.ContractCode}",
                    body,
                    to: toBoss,
                    timeoutSeconds: 15,
                    ct: ct
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CREATE contract => fallo notificando. ContractID={ContractID}", created?.ContractID);
        }
    }

    private async Task NotifyOnUpdateAsync(Contracts updated, CancellationToken ct)
    {
        try
        {
            await _currentUser.LoadBossAsync(ct);
            var toBoss = _currentUser.BossEmail?.Trim();

            if (!string.IsNullOrWhiteSpace(toBoss))
            {
                var body = BuildUpdateEmailBody(updated);
                await _emailBuilder.TryNotifyAsync(
                    EmailTemplateKey.AttendancePunch,
                    $"Contrato actualizado: {updated.ContractCode}",
                    body,
                    to: toBoss,
                    timeoutSeconds: 15,
                    ct: ct
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UPDATE contract => fallo notificando. ContractID={ContractID}", updated?.ContractID);
        }
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private static void ValidateDates(Contracts entity)
    {
        if (entity.EndDate < entity.StartDate)
            throw new Exception("EndDate no puede ser menor que StartDate.");
    }

    private static void ApplyDto(ContractsUpdateDto dto, Contracts target)
    {
        target.CertificationID = dto.CertificationID;
        target.ParentID = dto.ParentID;
        target.ContractCode = dto.ContractCode?.Trim() ?? target.ContractCode;

        target.PersonID = dto.PersonID;
        target.ContractTypeID = dto.ContractTypeID;
        target.JobID = dto.JobID;

        target.StartDate = dto.StartDate;
        target.EndDate = dto.EndDate;

        target.ContractFileName = dto.ContractFileName;
        target.ContractFilepath = dto.ContractFilepath;

        target.ContractDescription = dto.ContractDescription;

        target.DepartmentID = dto.DepartmentID;
        target.AuthorizationDate = dto.AuthorizationDate;

        target.ResignationFileName = dto.ResignationFileName;
        target.ResignationFilepath = dto.ResignationFilepath;
        target.ResignationCode = dto.ResignationCode;
        target.RegResignationDate = dto.RegResignationDate;
        target.ResignationDate = dto.ResignationDate;

        target.CancelReason = dto.CancelReason;
        target.CancelFilename = dto.CancelFilename;
        target.CancelFilepath = dto.CancelFilepath;
        target.CancelCode = dto.CancelCode;
        target.RegistrationDateAnulCon = dto.RegistrationDateAnulCon;

        target.Nationality = dto.Nationality;
        target.Visa = dto.Visa;
        target.Consulate = dto.Consulate;
        target.WorkOf = dto.WorkOf;

        target.InicialContent = dto.InicialContent;
        target.ResolucionContent = dto.ResolucionContent;

        target.RelationshipType = dto.RelationshipType;
        target.Relationship = dto.Relationship;

        target.Competition = dto.Competition;
        target.CompetitionDate = dto.CompetitionDate;
    }

    private static void CopyUpdatableFields(Contracts source, Contracts target)
    {
        target.CertificationID = source.CertificationID;
        target.ParentID = source.ParentID;
        target.ContractCode = source.ContractCode?.Trim() ?? target.ContractCode;

        target.PersonID = source.PersonID;
        target.ContractTypeID = source.ContractTypeID;
        target.JobID = source.JobID;

        target.StartDate = source.StartDate;
        target.EndDate = source.EndDate;

        target.ContractFileName = source.ContractFileName;
        target.ContractFilepath = source.ContractFilepath;

        target.Status = source.Status;
        target.ContractDescription = source.ContractDescription;

        target.DepartmentID = source.DepartmentID;
        target.AuthorizationDate = source.AuthorizationDate;

        target.ResignationFileName = source.ResignationFileName;
        target.ResignationFilepath = source.ResignationFilepath;
        target.ResignationCode = source.ResignationCode;
        target.RegResignationDate = source.RegResignationDate;
        target.ResignationDate = source.ResignationDate;

        target.CancelReason = source.CancelReason;
        target.CancelFilename = source.CancelFilename;
        target.CancelFilepath = source.CancelFilepath;
        target.CancelCode = source.CancelCode;
        target.RegistrationDateAnulCon = source.RegistrationDateAnulCon;

        target.Nationality = source.Nationality;
        target.Visa = source.Visa;
        target.Consulate = source.Consulate;
        target.WorkOf = source.WorkOf;

        target.InicialContent = source.InicialContent;
        target.ResolucionContent = source.ResolucionContent;

        target.RelationshipType = source.RelationshipType;
        target.Relationship = source.Relationship;

        target.Competition = source.Competition;
        target.CompetitionDate = source.CompetitionDate;
    }

    private static string BuildCreateEmailBody(Contracts c) => $@"
        <h3>Nuevo contrato creado</h3>
        <ul>
          <li><b>Contrato:</b> {c.ContractCode}</li>
          <li><b>PersonID:</b> {c.PersonID}</li>
          <li><b>Tipo:</b> {c.ContractTypeID}</li>
          <li><b>Departamento:</b> {c.DepartmentID}</li>
          <li><b>Inicio:</b> {c.StartDate:yyyy-MM-dd}</li>
          <li><b>Fin:</b> {c.EndDate:yyyy-MM-dd}</li>
          <li><b>Estado:</b> {c.Status}</li>
        </ul>";

    private static string BuildUpdateEmailBody(Contracts c) => $@"
        <h3>Contrato actualizado</h3>
        <ul>
          <li><b>Contrato:</b> {c.ContractCode}</li>
          <li><b>PersonID:</b> {c.PersonID}</li>
          <li><b>Tipo:</b> {c.ContractTypeID}</li>
          <li><b>Departamento:</b> {c.DepartmentID}</li>
          <li><b>Inicio:</b> {c.StartDate:yyyy-MM-dd}</li>
          <li><b>Fin:</b> {c.EndDate:yyyy-MM-dd}</li>
          <li><b>Estado:</b> {c.Status}</li>
        </ul>";

    public async Task<GenerateContractDocumentResponse> GenerateDocumentAsync(
    int contractId,
    GenerateContractDocumentRequest request,
    int generatedBy,
    CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contract = await _db.Set<Contracts>()
            .FirstOrDefaultAsync(x => x.ContractID == contractId, ct)
            ?? throw new KeyNotFoundException($"Contrato id={contractId} no existe.");

        if (contract.IsDocumentFrozen && !request.ForceRegenerate)
            throw new InvalidOperationException(
                "El documento del contrato está congelado. Use ForceRegenerate=true para regenerarlo.");

        var employeeId = await _db.Employees
            .AsNoTracking()
            .Where(e => e.PersonID == contract.PersonID)
            .Select(e => e.EmployeeId)
            .FirstOrDefaultAsync(ct);

        if (employeeId <= 0)
            throw new InvalidOperationException(
                $"No existe empleado asociado al PersonID={contract.PersonID}.");

        var templateId = await ResolveContractTemplateIdAsync(contract.ContractTypeID, ct);

        var generateRequest = new GenerateDocumentRequest(
            TemplateId: templateId,
            EmployeeId: employeeId,
            EntityType: DocumentEntityType.Contract,
            EntityId: contract.ContractID,
            DocumentNumber: contract.ContractCode,
            Notes: $"Documento generado para contrato {contract.ContractID}",
            ManualOverrides: request.Overrides
        );

        var document = await _documentGenerationService.GenerateAsync(
            generateRequest,
            generatedBy,
            ct);

        var template = await _templateRepository.GetByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException($"Plantilla {templateId} no encontrada.");

        await _repository.FreezeDocumentAsync(
            contract.ContractID,
            document.DocumentId,
            ParseTemplateVersion(template.Version),
            ct);

        var generatedStatusId = await GetContractStatusIdAsync(StatusGenerado, ct);

        contract.GeneratedDocumentId = document.DocumentId;
        contract.TemplateVersionUsed = ParseTemplateVersion(template.Version);
        contract.IsDocumentFrozen = true;
        contract.Status = generatedStatusId;
        contract.UpdatedAt = DateTime.Now;
        contract.UpdatedBy = generatedBy > 0 ? generatedBy : null;

        await _db.SaveChangesAsync(ct);

        await _documentGenerationService.UpdateStatusAsync(
            document.DocumentId,
            new UpdateDocumentStatusRequest(ToDocumentStatus(StatusGenerado), "Documento de contrato generado."),
            generatedBy,
            ct);

        return new GenerateContractDocumentResponse(
            ContractID: contract.ContractID,
            GeneratedDocumentId: document.DocumentId,
            DocumentNumber: document.DocumentNumber,
            FileName: document.FileName,
            PdfBase64: document.PdfBase64,
            FileSizeBytes: document.FileSizeBytes,
            IsDocumentFrozen: true,
            ContractStatus: generatedStatusId,
            ContractStatusName: StatusGenerado
        );
    }

    public async Task MarkDocumentPendingSignaturesAsync(
        int contractId,
        string? comment,
        int updatedBy,
        CancellationToken ct)
    {
        var contract = await GetContractWithGeneratedDocumentAsync(contractId, ct);

        await UpdateContractDocumentStatusAsync(
            contract,
            StatusPendienteFirmas,
            comment,
            updatedBy,
            ct);
    }

    public async Task UploadSignedDocumentAsync(
        int contractId,
        UploadSignedContractDocumentRequest request,
        int updatedBy,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.StoredFileId <= 0)
            throw new ArgumentException("StoredFileId es obligatorio.");

        var contract = await GetContractWithGeneratedDocumentAsync(contractId, ct);

        contract.SignedDocumentStoredFileId = request.StoredFileId;

        await UpdateContractDocumentStatusAsync(
            contract,
            StatusFirmadoCargado,
            request.Comment,
            updatedBy,
            ct);
    }

    public async Task FinalizeDocumentAsync(
        int contractId,
        string? comment,
        int updatedBy,
        CancellationToken ct)
    {
        var contract = await GetContractWithGeneratedDocumentAsync(contractId, ct);

        if (!contract.SignedDocumentStoredFileId.HasValue)
            throw new InvalidOperationException(
                "No se puede finalizar sin cargar el documento firmado.");

        await UpdateContractDocumentStatusAsync(
            contract,
            StatusFinalizado,
            comment,
            updatedBy,
            ct);
    }

    public async Task CancelDocumentAsync(
        int contractId,
        CancelContractDocumentRequest request,
        int updatedBy,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Debe ingresar el motivo de anulación.");

        var contract = await GetContractWithGeneratedDocumentAsync(contractId, ct);

        await UpdateContractDocumentStatusAsync(
            contract,
            StatusAnulado,
            request.Reason,
            updatedBy,
            ct);
    }

    private async Task UpdateContractDocumentStatusAsync(
        Contracts contract,
        string statusName,
        string? comment,
        int updatedBy,
        CancellationToken ct)
    {
        var statusId = await GetContractStatusIdAsync(statusName, ct);

        contract.Status = statusId;
        contract.UpdatedAt = DateTime.Now;
        contract.UpdatedBy = updatedBy > 0 ? updatedBy : null;

        await _db.SaveChangesAsync(ct);

        await _documentGenerationService.UpdateStatusAsync(
            contract.GeneratedDocumentId!.Value,
            new UpdateDocumentStatusRequest(ToDocumentStatus(statusName), comment),
            updatedBy,
            ct);
    }

    private async Task<int> GetContractStatusIdAsync(string statusName, CancellationToken ct)
    {
        var statusId = await _db.RefTypes
            .AsNoTracking()
            .Where(x =>
                x.Category == ContractStatusCategory &&
                x.Name == statusName &&
                x.IsActive)
            .Select(x => x.TypeId)
            .FirstOrDefaultAsync(ct);

        if (statusId <= 0)
            throw new InvalidOperationException(
                $"No existe el estado {ContractStatusCategory}/{statusName} en ref_Types.");

        return statusId;
    }

    /// <summary>Convierte el nombre de estado del contrato (español) al valor inglés
    /// que acepta la restricción CHK_GeneratedDocuments_Status.</summary>
    private static string ToDocumentStatus(string contractStatusName) => contractStatusName switch
    {
        "BORRADOR"         => "DRAFT",
        "GENERADO"         => "GENERATED",
        "PENDIENTE_FIRMAS" => "SIGNED",
        "FIRMADO_CARGADO"  => "SIGNED",
        "FINALIZADO"       => "APPROVED",
        "ANULADO"          => "REJECTED",
        _                  => "DRAFT"
    };

    private async Task<Contracts> GetContractWithGeneratedDocumentAsync(
        int contractId,
        CancellationToken ct)
    {
        var contract = await _db.Set<Contracts>()
            .FirstOrDefaultAsync(x => x.ContractID == contractId, ct)
            ?? throw new KeyNotFoundException($"Contrato id={contractId} no existe.");

        if (!contract.GeneratedDocumentId.HasValue)
            throw new InvalidOperationException(
                "El contrato no tiene documento generado.");

        return contract;
    }

    private async Task<int> ResolveContractTemplateIdAsync(
        int contractTypeId,
        CancellationToken ct)
    {
        var contractType = await _db.ContractType
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContractTypeId == contractTypeId, ct)
            ?? throw new KeyNotFoundException($"Tipo de contrato id={contractTypeId} no existe.");

        if (contractType.DefaultTemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(
                contractType.DefaultTemplateId.Value,
                ct)
                ?? throw new KeyNotFoundException(
                    $"La plantilla {contractType.DefaultTemplateId.Value} no existe.");

            if (template.Status != DocumentTemplateStatus.Published)
                throw new InvalidOperationException(
                    $"La plantilla '{template.Name}' no está publicada.");

            return template.TemplateId;
        }

        var templates = await _templateRepository.GetAllAsync(
            templateType: "CONTRATO",
            status: DocumentTemplateStatus.Published,
            ct: ct);

        if (templates.Count == 0)
            throw new InvalidOperationException(
                "No existe una plantilla publicada de tipo CONTRATO.");

        return templates[0].TemplateId;
    }

    private static int ParseTemplateVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return 1;

        var major = version.Split('.', StringSplitOptions.RemoveEmptyEntries)[0];

        return int.TryParse(major, out var value) && value > 0
            ? value
            : 1;
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Contracts;
using WsUtaSystem.Application.DTOs.ContractStatusHistory;
using WsUtaSystem.Application.DTOs.Provisioning;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
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
    private const string StatusVigente = "VIGENTE";
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
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<ContractsService> _logger;
    private readonly IParametersRepository _parametersRepository;
    // Orquestador reutilizable: centraliza EnsureEmployee + RepositoryUta + UpdateEmail + SendEmail
    private readonly IEmployeeProvisioningOrchestrator _provisioningOrchestrator;

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
        IHttpContextAccessor httpContext,
        ILogger<ContractsService> logger,
        IParametersRepository parametersRepository,
        IEmployeeProvisioningOrchestrator provisioningOrchestrator
    ) : base(repo)
    {
        _repository                = repo                       ?? throw new ArgumentNullException(nameof(repo));
        _db                        = db                         ?? throw new ArgumentNullException(nameof(db));
        _emailBuilder              = emailBuilder               ?? throw new ArgumentNullException(nameof(emailBuilder));
        _currentUser               = currentUser                ?? throw new ArgumentNullException(nameof(currentUser));
        _employeeDetails           = employeeDetails            ?? throw new ArgumentNullException(nameof(employeeDetails));
        _refTypes                  = refTypes                   ?? throw new ArgumentNullException(nameof(refTypes));
        _templateRepository        = templateRepository         ?? throw new ArgumentNullException(nameof(templateRepository));
        _documentGenerationService = documentGenerationService  ?? throw new ArgumentNullException(nameof(documentGenerationService));
        _contractTypeRepository    = contractTypeRepository     ?? throw new ArgumentNullException(nameof(contractTypeRepository));
        _httpContext               = httpContext                ?? throw new ArgumentNullException(nameof(httpContext));
        _logger                    = logger                     ?? throw new ArgumentNullException(nameof(logger));
        _parametersRepository      = parametersRepository       ?? throw new ArgumentNullException(nameof(parametersRepository));
        _provisioningOrchestrator  = provisioningOrchestrator  ?? throw new ArgumentNullException(nameof(provisioningOrchestrator));
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

        // Auto-poblar régimen, modalidad y horas desde la cadena CertificationID → ContractRequest
        // Solo si los campos no vienen ya explícitamente en el DTO
        if (isRootContract && entity.CertificationID.HasValue
            && (entity.LaborRegimeID is null || entity.WorkModalityID is null || entity.ContractedHours is null))
        {
            var requestData = await _db.Set<FinancialCertification>()
                .AsNoTracking()
                .Where(f => f.CertificationId == entity.CertificationID.Value)
                .Select(f => new
                {
                    f.Request!.WorkModalityId,
                    f.Request!.NumberHour,
                })
                .FirstOrDefaultAsync(ct);

            if (requestData is not null)
            {
                entity.WorkModalityID  ??= requestData.WorkModalityId;
                entity.ContractedHours ??= requestData.NumberHour > 0 ? requestData.NumberHour : null;
            }

            // LaborRegimeID: derivar desde PersonalContractTypeId del tipo de contrato
            if (entity.LaborRegimeID is null && entity.ContractTypeID > 0)
            {
                var contractType = await _db.Set<ContractType>()
                    .AsNoTracking()
                    .Where(ct => ct.ContractTypeId == entity.ContractTypeID)
                    .Select(ct => ct.PersonalContractTypeId)
                    .FirstOrDefaultAsync(ct);

                // PersonalContractTypeId ya migrado a CONTRACT_TYPE: 57=LOSEP, 58=LOES, 59=CT
                entity.LaborRegimeID = contractType switch
                {
                    57 => 57, // LOSEP
                    58 => 58, // LOES
                    59 => 59, // Código del Trabajo
                    _  => contractType  // cualquier otro valor, guardar directo
                };
            }
        }

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

            var fromStatus = contract.Status;
            if (fromStatus == toStatusTypeId)
                return; // NoOp

            var allowed = await _db.ContractStatusTransitions
                .AsNoTracking()
                .AnyAsync(x => x.IsActive && x.FromStatusTypeID == fromStatus && x.ToStatusTypeID == toStatusTypeId, ct);

            if (!allowed)
                throw new InvalidOperationException($"Transición no permitida: {fromStatus} -> {toStatusTypeId}");

            contract.Status = toStatusTypeId;

            var userId = _currentUser.EmployeeId;
            _db.ContractStatusHistories.Add(new ContractStatusHistory
            {
                ContractID = contractId,
                StatusTypeID = toStatusTypeId,
                Comment = comment,
                ChangedBy = userId,
                ChangedAt = DateTime.Now
            });

            // Si se anula un contrato raíz, revertir contadores y persona de la solicitud
            var toStatusName = await _db.RefTypes
                .AsNoTracking()
                .Where(x => x.TypeId == toStatusTypeId && x.Category == ContractStatusCategory)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct);

            if (toStatusName == StatusAnulado && contract.ParentID is null)
                await ReverseContractRequestOnCancellationAsync(contractId, contract.CertificationID, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    /// <summary>
    /// Dispara el aprovisionamiento AD para el contrato indicado.
    /// Retorna true solo si la cuenta fue creada exitosamente en esta llamada.
    /// Si la cuenta ya existía (409) o hubo error, retorna false sin propagar excepción.
    /// </summary>
    /// <summary>
    /// Dispara el aprovisionamiento de cuenta institucional para el empleado
    /// asociado al contrato. Delega toda la lógica al <see cref="IEmployeeProvisioningOrchestrator"/>.
    /// <para>
    /// Retorna (true, provisioningId) si la cuenta fue creada exitosamente en AD Local.
    /// Retorna (false, null) si el aprovisionamiento falló o la cuenta ya existía.
    /// </para>
    /// </summary>
    private async Task<(bool Provisioned, Guid? ProvisioningId)> TriggerProvisioningAsync(
        int personId, int departmentId, int contractId, int updatedBy, CancellationToken ct)
    {
        // Leer nombre del departamento para pasarlo al orquestador
        string? deptName = null;
        if (departmentId > 0)
            deptName = await _db.Departments
                .AsNoTracking()
                .Where(d => d.DepartmentId == departmentId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync(ct);

        // Tipo de empleado: se lee desde el registro existente (si hay) o se usa 0 como fallback
        // (el orquestador creará tbl_Employees con este tipo si no existe el registro)
        var empType = await _db.Employees
            .AsNoTracking()
            .Where(e => e.PersonID == personId)
            .Select(e => (int?)e.EmployeeType)
            .FirstOrDefaultAsync(ct) ?? 0;

        // JobId del contrato que se está firmando
        var contractJobId = await _db.Contracts
            .AsNoTracking()
            .Where(c => c.ContractID == contractId)
            .Select(c => c.JobID)
            .FirstOrDefaultAsync(ct);

        var token = _httpContext.HttpContext?.Request.Headers["Authorization"].FirstOrDefault()
            ?? string.Empty;

        var request = new ProvisioningOrchestrationRequest(
            PersonId:        personId,
            EmployeeType:    empType,
            DepartmentId:    departmentId > 0 ? departmentId : null,
            DepartmentName:  deptName,
            HireDate:        null,
            JobId:           contractJobId,
            UpdatedBy:       updatedBy,
            BearerToken:     token,
            SourceReference: $"Contract:{contractId}",
            Source:          ProvisioningSource.Contract
        );

        var result = await _provisioningOrchestrator.ExecuteAsync(request, ct);

        // El orquestador ya loguea todo el detalle; aquí solo registramos el resultado final
        if (!result.Success && !result.AlreadyExists)
        {
            _logger.LogWarning(
                "[CONTRACT] Aprovisionamiento no exitoso para PersonID={PersonId} ContractId={ContractId}: {Error}",
                personId, contractId, result.ErrorMessage);
            return (false, null);
        }

        // Si el aprovisionamiento creó un empleado nuevo, actualizar EmployeeId en el documento generado
        if (result.EmployeeId.HasValue)
        {
            var generatedDocId = await _db.Contracts
                .AsNoTracking()
                .Where(c => c.ContractID == contractId)
                .Select(c => c.GeneratedDocumentId)
                .FirstOrDefaultAsync(ct);

            if (generatedDocId.HasValue)
            {
                await _db.GeneratedDocuments
                    .Where(d => d.DocumentId == generatedDocId.Value)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(d => d.EmployeeId, result.EmployeeId.Value)
                        .SetProperty(d => d.UpdatedAt, DateTime.UtcNow),
                    ct);

                _logger.LogInformation(
                    "[CONTRACT] ✓ EmployeeId actualizado en documento. ContractId={ContractId} | DocumentId={DocId} | EmployeeId={EmployeeId}",
                    contractId, generatedDocId.Value, result.EmployeeId.Value);
            }
        }

        return (result.Success, null);
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
            .Select(e => (int?)e.EmployeeId)
            .FirstOrDefaultAsync(ct);

        var templateId = await ResolveContractTemplateIdAsync(contract.ContractTypeID, ct);

        // Resolver responsables del contrato para variables de plantilla
        var (authorityName, authorityTitle) = await ResolveEmployeeInfoAsync(contract.AuthorityNominatorId, ct);
        var (directorName, directorTitle)   = await ResolveEmployeeInfoAsync(contract.DthDirectorId, ct);
        var (registrarName, _)              = await ResolveEmployeeInfoAsync(contract.CreatedBy, ct);

        var mergedOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static void SetOv(Dictionary<string, string> d, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) d[key] = value!;
        }
        SetOv(mergedOverrides, "DTH_DIRECTOR_NAME",     directorName);
        SetOv(mergedOverrides, "DTH_DIRECTOR_FULLNAME", directorName);
        SetOv(mergedOverrides, "DTH_DIRECTOR_TITLE",    directorTitle);
        SetOv(mergedOverrides, "AUTHORITY_NAME",        authorityName);
        SetOv(mergedOverrides, "AUTHORITY_TITLE",       authorityTitle);
        SetOv(mergedOverrides, "REGISTRAR_NAME",        registrarName);

        // Los overrides manuales del request tienen máxima prioridad
        if (request.Overrides is not null)
            foreach (var kvp in request.Overrides)
                mergedOverrides[kvp.Key] = kvp.Value;

        var generateRequest = new GenerateDocumentRequest(
            TemplateId:      templateId,
            EmployeeId:      employeeId > 0 ? employeeId : null,
            EntityType:      DocumentEntityType.Contract,
            EntityId:        contract.ContractID,
            DocumentNumber:  contract.ContractCode,
            Notes:           $"Documento generado para contrato {contract.ContractID}",
            ManualOverrides: mergedOverrides.Count > 0 ? mergedOverrides : null,
            PersonId:        contract.PersonID
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

        // Disparar aprovisionamiento AD si el tipo de contrato lo requiere (solo contratos raíz)
        if (contract.ParentID is null)
        {
            var contractType = await _db.ContractType
                .AsNoTracking()
                .Where(x => x.ContractTypeId == contract.ContractTypeID)
                .Select(x => new { x.RequiresAdUserCreation })
                .FirstOrDefaultAsync(ct);

            if (contractType?.RequiresAdUserCreation == true)
            {
                var (provisioned, provisioningId) = await TriggerProvisioningAsync(
                    contract.PersonID, contract.DepartmentID, contractId, updatedBy, ct);

                if (provisioned)
                    await UpdateContractDocumentStatusAsync(
                        contract, StatusVigente,
                        "Cuenta institucional creada automáticamente al cargar documento firmado.",
                        updatedBy, ct);
            }
        }
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

        var contract = await _db.Set<Contracts>()
            .FirstOrDefaultAsync(x => x.ContractID == contractId, ct)
            ?? throw new KeyNotFoundException($"Contrato id={contractId} no existe.");

        var statusId = await GetContractStatusIdAsync(StatusAnulado, ct);
        contract.Status    = statusId;
        contract.UpdatedAt = DateTime.Now;
        contract.UpdatedBy = updatedBy > 0 ? updatedBy : null;
        await _db.SaveChangesAsync(ct);

        // Solo actualizar el documento si ya fue generado (BORRADOR no tiene documento)
        if (contract.GeneratedDocumentId.HasValue)
        {
            await _documentGenerationService.UpdateStatusAsync(
                contract.GeneratedDocumentId.Value,
                new UpdateDocumentStatusRequest(ToDocumentStatus(StatusAnulado), request.Reason),
                updatedBy,
                ct);
        }

        // Revertir contadores y estado de persona en la solicitud (solo contrato raíz)
        if (contract.ParentID is null)
            await ReverseContractRequestOnCancellationAsync(contractId, contract.CertificationID, ct);
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

        // Auditoría: registrar el cambio de estado en el historial
        _db.ContractStatusHistories.Add(new ContractStatusHistory
        {
            ContractID  = contract.ContractID,
            StatusTypeID = statusId,
            Comment     = comment,
            ChangedBy   = updatedBy > 0 ? updatedBy : _currentUser.EmployeeId,
            ChangedAt   = DateTime.Now,
        });

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
        "VIGENTE"          => "APPROVED",
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

    /// <summary>
    /// Revierte los contadores y el estado de la persona en la solicitud de contrato
    /// cuando un contrato raíz es anulado.
    /// </summary>
    private async Task ReverseContractRequestOnCancellationAsync(
        int contractId,
        int? certificationId,
        CancellationToken ct)
    {
        // 1. Liberar la persona vinculada al contrato en la solicitud
        var person = await _db.Set<ContractRequestPerson>()
            .FirstOrDefaultAsync(x => x.ContractId == contractId, ct);

        if (person is not null)
        {
            var pendientePersonStatusId = await _db.RefTypes
                .AsNoTracking()
                .Where(x => x.Category == "CONTRACT_REQUEST_PERSON_STATUS"
                         && x.Name == "PENDIENTE"
                         && x.IsActive)
                .Select(x => (int?)x.TypeId)
                .FirstOrDefaultAsync(ct);

            person.IsHired    = false;
            person.ContractId = null;
            person.UpdatedAt  = DateTime.Now;
            person.UpdatedBy  = _currentUser.EmployeeId;

            if (pendientePersonStatusId.HasValue)
                person.StatusId = pendientePersonStatusId.Value;
        }

        // 2. Decrementar contador de contratados en la solicitud
        if (certificationId is null)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var cert = await _db.Set<FinancialCertification>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CertificationId == certificationId.Value, ct);

        if (cert?.RequestId is null)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        var contractRequest = await _db.Set<ContractRequest>()
            .FirstOrDefaultAsync(x => x.RequestId == cert.RequestId.Value, ct);

        if (contractRequest is null)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        contractRequest.TotalPeopleHired = Math.Max(0, contractRequest.TotalPeopleHired - 1);
        contractRequest.UpdatedAt = DateTime.Now;

        // Nuevo estado según contadores
        var newRequestStatusName = contractRequest.TotalPeopleHired <= 0
            ? "PENDIENTE"
            : contractRequest.TotalPeopleHired >= contractRequest.NumberOfPeopleToHire
                ? "COMPLETADO"
                : "EN_PROCESO";

        var newStatusId = await _db.RefTypes
            .AsNoTracking()
            .Where(x => x.Category == "CONTRACT_REQUEST_STATUS"
                     && x.Name == newRequestStatusName
                     && x.IsActive)
            .Select(x => (int?)x.TypeId)
            .FirstOrDefaultAsync(ct);

        // Si "PENDIENTE" no existe en ref_Types, caer a EN_PROCESO
        if (!newStatusId.HasValue && newRequestStatusName == "PENDIENTE")
        {
            newStatusId = await _db.RefTypes
                .AsNoTracking()
                .Where(x => x.Category == "CONTRACT_REQUEST_STATUS"
                         && x.Name == "EN_PROCESO"
                         && x.IsActive)
                .Select(x => (int?)x.TypeId)
                .FirstOrDefaultAsync(ct);
        }

        if (newStatusId.HasValue)
            contractRequest.Status = newStatusId.Value;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<(string? Name, string? JobTitle)> ResolveEmployeeInfoAsync(int? employeeId, CancellationToken ct)
    {
        if (!employeeId.HasValue) return (null, null);

        var row = await (
            from emp in _db.Employees.AsNoTracking()
            join person in _db.People.AsNoTracking()
                on emp.PersonID equals person.PersonId
            join job in _db.Jobs.AsNoTracking()
                on emp.JobId equals job.JobID into jobJoin
            from job in jobJoin.DefaultIfEmpty()
            where emp.EmployeeId == employeeId.Value
            select new
            {
                Name     = person.FirstName + " " + person.LastName,
                JobTitle = (string?)job.Description
            }
        ).FirstOrDefaultAsync(ct);

        return row is null ? (null, null) : (row.Name, row.JobTitle);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContractReportDto>> GetForReportAsync(ReportFilterDto filter, CancellationToken ct = default)
    {
        var query =
            from c in _db.Contracts.AsNoTracking()
            join p    in _db.People.AsNoTracking()         on c.PersonID        equals p.PersonId
            join d    in _db.Departments.AsNoTracking()    on c.DepartmentID    equals d.DepartmentId
            join ct2  in _db.ContractType.AsNoTracking()   on c.ContractTypeID  equals ct2.ContractTypeId
            join rs   in _db.RefTypes.AsNoTracking()       on c.Status          equals rs.TypeId  into rsg
            from rs   in rsg.DefaultIfEmpty()
            join lr   in _db.RefTypes.AsNoTracking()       on c.LaborRegimeID   equals lr.TypeId  into lrg
            from lr   in lrg.DefaultIfEmpty()
            join wm   in _db.RefTypes.AsNoTracking()       on c.WorkModalityID  equals wm.TypeId  into wmg
            from wm   in wmg.DefaultIfEmpty()
            join j    in _db.Jobs.AsNoTracking()           on c.JobID           equals j.JobID    into jg
            from j    in jg.DefaultIfEmpty()
            join cbe  in _db.Employees.AsNoTracking()      on c.CreatedBy       equals cbe.EmployeeId into cbeg
            from cbe  in cbeg.DefaultIfEmpty()
            join cbp  in _db.People.AsNoTracking()         on cbe.PersonID      equals cbp.PersonId   into cbpg
            from cbp  in cbpg.DefaultIfEmpty()
            where (!filter.StartDate.HasValue         || c.StartDate >= filter.StartDate.Value)
               && (!filter.EndDate.HasValue           || c.StartDate <= filter.EndDate.Value)
               && (!filter.DepartmentId.HasValue      || c.DepartmentID    == filter.DepartmentId.Value)
               && (!filter.ContractTypeId.HasValue    || c.ContractTypeID  == filter.ContractTypeId.Value)
               && (!filter.LaborRegimeId.HasValue     || c.LaborRegimeID   == filter.LaborRegimeId.Value)
               && (!filter.CreatedByEmployeeId.HasValue || c.CreatedBy     == filter.CreatedByEmployeeId.Value)
               && (string.IsNullOrEmpty(filter.Status) || (rs != null && rs.Name == filter.Status))
            orderby c.StartDate descending
            select new ContractReportDto
            {
                ContractId        = c.ContractID,
                ContractCode      = c.ContractCode,
                PersonIdCard      = p.IdCard,
                PersonFullName    = p.FirstName + " " + p.LastName,
                DepartmentName    = d.Name,
                ContractTypeName  = ct2.Name,
                LaborRegimeName   = lr != null ? lr.Name : null,
                WorkModalityName  = wm != null ? wm.Name : null,
                ContractedHours   = c.ContractedHours,
                JobTitle          = j != null ? j.Description : null,
                CreatedByName     = cbp != null ? cbp.FirstName + " " + cbp.LastName : null,
                StartDate         = c.StartDate,
                EndDate           = c.EndDate,
                StatusName        = rs != null ? rs.Name : "—"
            };

        return await query.ToListAsync(ct);
    }
}

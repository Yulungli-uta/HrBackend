using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
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
using WsUtaSystem.Reports.Engine;

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

    // HR.ref_Types (Category=DOCUMENT_TYPE) — documentos cargados en TBL_StoredFile cuyo
    // número/fecha alimentan placeholders de plantillas de contrato. Se resuelven por NOMBRE,
    // nunca por TypeId fijo: el TypeId es IDENTITY y puede variar entre ambientes; el Name es
    // el dato estable de la semilla.
    private const string DocumentTypeNameResolucionCau = "RESOLUCION_CAU";
    private const string DocumentTypeNameMemorandoRectorado = "MEMORANDO_RECTORADO";
    private const string DocumentTypeNameResolucionDelegacion = "RESOLUCION_DELEGACION";

    // HR.ref_Types (Category=ACCESS_MODULE_TYPE) — módulo de este servicio para el checklist
    // de requisitos documentales (HR.tbl_TramiteRequirements), resuelto por Name.
    private const string ModuleTypeNameContracts = "CONTRACTS";

    // HR.tbl_StoredFile.EntityType usado para documentos de contrato. DEBE coincidir con
    // CONTRACT_ENTITY_TYPE del frontend (client/src/features/constants.ts) — verificado
    // contra datos reales: HR.tbl_StoredFile solo contiene filas con "HRCONTRACT", nunca
    // "CONTRACT" (bug encontrado 2026-07-20: 4 lookups usaban el string equivocado y por
    // eso ValidateRequiredDocumentsAsync nunca encontraba documentos ya subidos).
    private const string ContractEntityType = "HRCONTRACT";

    // HR.ref_Types (Category=DEPARTMENT_TYPE) — usado para resolver la Facultad real de una
    // autoridad delegada, subiendo por ParentId si el registro quedó a nivel de Carrera/Dirección.
    private const int DepartmentTypeIdFacultad = 128;

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
    private readonly IEmployeeLaborRegimeService _laborRegimeService;
    private readonly IPersonnelMovementsService _movementsService;
    private readonly ITramiteRequirementsService _tramiteRequirements;
    private readonly ISalaryHistoryService _salaryHistory;

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
        IEmployeeProvisioningOrchestrator provisioningOrchestrator,
        IEmployeeLaborRegimeService laborRegimeService,
        IPersonnelMovementsService movementsService,
        ITramiteRequirementsService tramiteRequirements,
        ISalaryHistoryService salaryHistory
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
        _laborRegimeService        = laborRegimeService         ?? throw new ArgumentNullException(nameof(laborRegimeService));
        _movementsService          = movementsService           ?? throw new ArgumentNullException(nameof(movementsService));
        _tramiteRequirements       = tramiteRequirements        ?? throw new ArgumentNullException(nameof(tramiteRequirements));
        _salaryHistory             = salaryHistory              ?? throw new ArgumentNullException(nameof(salaryHistory));
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

            // El frontend ya oculta el botón de edición fuera de BORRADOR/GENERADO
            // (ContractDetail.tsx: isEditable), pero el backend no lo exigía — cualquier
            // llamada directa al endpoint podía editar un contrato ya VIGENTE. Mismo guard
            // que ya tenía PersonnelActionService.UpdateAsync. Bug de seguridad encontrado y
            // corregido 2026-08-05 junto con la feature de corrección (que sí permite editar
            // en cualquier estado a propósito, porque exige motivo y queda auditada — ver
            // CorrectAsync).
            var currentStatusName = await _db.RefTypes
                .AsNoTracking()
                .Where(x => x.TypeId == current.Status && x.Category == ContractStatusCategory)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct);

            var editableStatuses = new[] { StatusBorrador, StatusGenerado };
            if (currentStatusName is not null && !editableStatuses.Contains(currentStatusName))
                throw new InvalidOperationException(
                    $"Solo se puede editar en estado BORRADOR o GENERADO. Estado actual: '{currentStatusName}'.");

            // ✅ Update campo por campo (en Service, buena práctica)
            ApplyDto(dto, current);

            await ValidateContractCodeUniqueAsync(current.ContractID, current.ContractCode, ct);
            ValidateDates(current);

            // Checklist de documentos obligatorios (HR.tbl_TramiteRequirements) — se valida aquí
            // (al guardar), no solo al generar el documento, para que el usuario se entere de
            // inmediato si falta algo en vez de descubrirlo recién al intentar generar el PDF.
            var contractsModuleTypeId = await _db.RefTypes
                .AsNoTracking()
                .Where(r => r.Category == "ACCESS_MODULE_TYPE" && r.Name == ModuleTypeNameContracts && r.IsActive)
                .Select(r => (int?)r.TypeId)
                .FirstOrDefaultAsync(ct);

            if (contractsModuleTypeId.HasValue)
            {
                await _tramiteRequirements.ValidateRequiredDocumentsAsync(
                    contractsModuleTypeId.Value, current.ContractTypeID, ContractEntityType, current.ContractID.ToString(), ct);
            }

            // Si manejas RowVersion en tu arquitectura, aquí va el OriginalValue:
            // _db.Entry(current).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

            await base.UpdateAsync(id, current, ct);

            updated = await _repository.GetByIdAsync(id, ct);

            await tx.CommitAsync(ct);
        });

    }

    /// <inheritdoc />
    public async Task CorrectAsync(int id, ContractsUpdateDto dto, string reason, CancellationToken ct)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Debe ingresar el motivo de la corrección.", nameof(reason));
        if (dto.ContractID != 0 && dto.ContractID != id)
            throw new ArgumentException("ContractID del body no coincide con el id de la ruta.");

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var current = await _repository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Contracts con id={id} no existe.");

            var before = AuditSnapshotHelper.Snapshot(current);

            // A diferencia de UpdateAsync, la corrección se permite en cualquier estado
            // (incluido VIGENTE) — exige motivo obligatorio y queda auditada en HR.Audit.
            ApplyDto(dto, current);
            await ValidateContractCodeUniqueAsync(current.ContractID, current.ContractCode, ct);
            ValidateDates(current);

            await base.UpdateAsync(id, current, ct);

            // Corrige (o crea si aún no existía) la fila de SalaryHistory ligada
            // específicamente a ESTE contrato — nunca la de otro contrato/acción
            // del mismo empleado.
            await RecordSalaryHistoryForContractAsync(current, $"Corrección de contrato: {reason}", ct);

            var after = AuditSnapshotHelper.Snapshot(current);
            await AuditSnapshotHelper.WriteCorrectionAuditAsync(
                _db, "Contracts", id.ToString(), reason, before, after,
                _currentUser.UserName ?? _currentUser.Email, ct);

            await tx.CommitAsync(ct);
        });
    }

    /// <inheritdoc />
    public async Task<int?> ResolvePersonIdByEmployeeIdAsync(int employeeId, CancellationToken ct)
    {
        return await _db.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .Select(e => (int?)e.PersonID)
            .FirstOrDefaultAsync(ct);
    }

    // -------------------------------------------------------
    // Métodos de negocio existentes (entity)
    // -------------------------------------------------------
    public async Task<Contracts> CreateAndNotifyAsync(Contracts entity, CancellationToken ct, bool isHistoricalEntry = false)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));
        ValidateDates(entity);

        if (isHistoricalEntry)
        {
            var today = DateTime.Today;
            if (entity.StartDate >= today)
                throw new ArgumentException("Un contrato histórico debe tener fecha de inicio anterior a hoy.");
            if (entity.EndDate.Year < 9999 && entity.EndDate >= today)
                throw new ArgumentException("Un contrato histórico debe tener fecha de fin anterior a hoy.");
        }

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

        // Registrar el régimen laboral del contrato (no bloquea la creación del contrato si falla).
        if (isRootContract && created is not null && created.LaborRegimeID.HasValue)
            await RegisterLaborRegimeFromContractAsync(created, ct);

        // Registrar el movimiento (aplica a raíz y adendum) si cambia de departamento.
        if (created is not null)
            await RegisterMovementFromContractAsync(created, ct);

        return created!;
    }

    /// <summary>
    /// Registra un movimiento de personal cuando el departamento de este contrato/adendum
    /// difiere del departamento del contrato/adendum inmediatamente anterior de la persona
    /// (o de <c>Employees.DepartmentId</c> si no tiene ninguno previo). Si es el mismo
    /// departamento, no se crea nada — no hubo cambio real. No bloquea la creación del contrato.
    /// </summary>
    private async Task RegisterMovementFromContractAsync(Contracts contract, CancellationToken ct)
    {
        try
        {
            var employeeId = await _db.Employees
                .AsNoTracking()
                .Where(e => e.PersonID == contract.PersonID && e.IsActive)
                .Select(e => e.EmployeeId)
                .FirstOrDefaultAsync(ct);

            if (employeeId == 0)
            {
                _logger.LogInformation(
                    "[MOVEMENT] Registro omitido: sin empleado activo para PersonID={PersonID} (ContractID={ContractID}).",
                    contract.PersonID, contract.ContractID);
                return;
            }

            if (!contract.JobID.HasValue)
            {
                _logger.LogInformation(
                    "[MOVEMENT] Registro omitido: contrato sin JobID. ContractID={ContractID}.",
                    contract.ContractID);
                return;
            }

            var previousDepartmentId = await _db.Set<Contracts>()
                .AsNoTracking()
                .Where(c => c.PersonID == contract.PersonID && c.ContractID != contract.ContractID)
                .OrderByDescending(c => c.StartDate)
                .ThenByDescending(c => c.ContractID)
                .Select(c => (int?)c.DepartmentID)
                .FirstOrDefaultAsync(ct);

            var originDepartmentId = previousDepartmentId ?? await _db.Employees
                .AsNoTracking()
                .Where(e => e.EmployeeId == employeeId)
                .Select(e => e.DepartmentId)
                .FirstOrDefaultAsync(ct);

            if (originDepartmentId == contract.DepartmentID)
                return; // mismo departamento — no hubo cambio real

            var movementTypeId = await _db.RefTypes
                .AsNoTracking()
                .Where(r => r.Category == "MOVEMENT_TYPE" && r.Name == "CONTRATO" && r.IsActive)
                .Select(r => (int?)r.TypeId)
                .FirstOrDefaultAsync(ct);

            await _movementsService.CreateAsync(new PersonnelMovements
            {
                EmployeeId = employeeId,
                ContractId = contract.ContractID,
                JobId = contract.JobID.Value,
                OriginDepartmentId = originDepartmentId,
                DestinationDepartmentId = contract.DepartmentID,
                MovementDate = DateOnly.FromDateTime(contract.StartDate),
                MovementTypeId = movementTypeId,
                IsActive = true,
                CreatedBy = _currentUser.EmployeeId,
                CreatedAt = DateTime.Now,
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MOVEMENT] ERROR registrando movimiento para ContractID={ContractID}.", contract.ContractID);
        }
    }

    /// <summary>
    /// Da de alta (o deja igual si ya existe activo) el régimen laboral del empleado a partir
    /// de un contrato raíz recién creado. No crea cuenta AD ni empleado nuevo: si el empleado
    /// activo aún no existe (aprovisionamiento pendiente por otro flujo), se omite en silencio.
    /// </summary>
    private async Task RegisterLaborRegimeFromContractAsync(Contracts contract, CancellationToken ct)
    {
        try
        {
            var employeeId = await _db.Employees
                .AsNoTracking()
                .Where(e => e.PersonID == contract.PersonID && e.IsActive)
                .Select(e => e.EmployeeId)
                .FirstOrDefaultAsync(ct);

            if (employeeId == 0)
            {
                _logger.LogInformation(
                    "[LABOR-REGIME] Registro omitido: sin empleado activo para PersonID={PersonID} (ContractID={ContractID}).",
                    contract.PersonID, contract.ContractID);
                return;
            }

            await _laborRegimeService.CreateAsync(new DTOs.EmployeeLaborRegime.EmployeeLaborRegimeCreateDto
            {
                EmployeeId = employeeId,
                LaborRegimeId = contract.LaborRegimeID!.Value,
                DepartmentId = contract.DepartmentID,
                JobId = contract.JobID,
                IsIndefinite = false, // los contratos siempre son a plazo; el nombramiento se registra vía acción de personal
                DocumentType = "CONTRACT",
                DocumentNumber = contract.ContractCode,
                SourceContractId = contract.ContractID,
                EffectiveFrom = DateOnly.FromDateTime(contract.StartDate),
            }, _currentUser.EmployeeId, ct);
        }
        catch (InvalidOperationException ex)
        {
            // Ya tiene ese régimen activo (ej. addendum o reintento) — no es un error de negocio bloqueante.
            _logger.LogInformation(ex,
                "[LABOR-REGIME] Registro omitido para ContractID={ContractID}: {Message}",
                contract.ContractID, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[LABOR-REGIME] ERROR registrando régimen laboral para ContractID={ContractID}.",
                contract.ContractID);
        }
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

            var userId = _currentUser.EmployeeId;
            await PersistStatusChangeAsync(contract, toStatusTypeId, comment, userId, ct);

            // Si se anula un contrato raíz, revertir contadores y persona de la solicitud
            var toStatusName = await _db.RefTypes
                .AsNoTracking()
                .Where(x => x.TypeId == toStatusTypeId && x.Category == ContractStatusCategory)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(ct);

            if (toStatusName == StatusAnulado && contract.ParentID is null)
                await ReverseContractRequestOnCancellationAsync(contractId, contract.CertificationID, ct);

            // 2026-07-06: mismo control que en UploadSignedDocumentAsync — si este
            // cambio de estado manual formaliza un adendum, anula a su padre.
            if ((toStatusName == StatusFirmadoCargado || toStatusName == StatusVigente) && contract.ParentID.HasValue)
                await AnnulParentContractAsync(contract.ParentID.Value, contractId, userId ?? 0, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    /// <summary>
    /// Núcleo compartido entre el flujo normal (<see cref="ChangeStatusAsync"/>, que valida
    /// <c>HR.tbl_ContractStatusTransition</c> y dispara efectos secundarios) y la corrección
    /// manual (<see cref="CorrectStatusAsync"/>, que no valida ni dispara nada más que esto):
    /// persiste el nuevo Status y agrega la fila de historial (sin SaveChanges — el llamador
    /// decide cuándo guardar/commitear).
    /// </summary>
    private async Task PersistStatusChangeAsync(Contracts contract, int toStatusTypeId, string? comment, int? changedBy, CancellationToken ct)
    {
        contract.Status = toStatusTypeId;

        _db.ContractStatusHistories.Add(new ContractStatusHistory
        {
            ContractID = contract.ContractID,
            StatusTypeID = toStatusTypeId,
            Comment = comment,
            ChangedBy = changedBy,
            ChangedAt = DateTime.Now
        });

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task CorrectStatusAsync(int contractId, int toStatusTypeId, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Debe ingresar el motivo de la corrección.", nameof(reason));

        var validStatus = await _db.RefTypes.AsNoTracking()
            .AnyAsync(x => x.TypeId == toStatusTypeId && x.Category == ContractStatusCategory, ct);
        if (!validStatus)
            throw new ArgumentException($"TypeId {toStatusTypeId} no es un estado válido de contrato.", nameof(toStatusTypeId));

        var contract = await _db.Set<Contracts>().FirstOrDefaultAsync(x => x.ContractID == contractId, ct)
            ?? throw new KeyNotFoundException($"Contrato id={contractId} no existe.");

        if (contract.Status == toStatusTypeId)
            return; // No-op: ya está en el estado solicitado.

        var fromStatus = contract.Status;

        // Corrección manual: a diferencia de ChangeStatusAsync, NO valida
        // tbl_ContractStatusTransition (la corrección puede necesitar ir a cualquier estado) y
        // NO dispara ningún efecto secundario (reversar cupo de solicitud, anular contrato
        // padre, etc.) — esos solo ocurren al avanzar un contrato por el flujo normal, nunca al
        // corregir el registro de uno ya existente.
        await PersistStatusChangeAsync(contract, toStatusTypeId, $"CORRECCIÓN: {reason.Trim()}", _currentUser.EmployeeId, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ContractsService: contrato {ContractId} — Status CORREGIDO de '{From}' a '{To}' por EmployeeId={UserId}. Motivo: {Reason}",
            contractId, fromStatus, toStatusTypeId, _currentUser.EmployeeId, reason);
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

    /// <summary>
    /// ContractCode es el número de documento oficial — respaldado por índice único
    /// (UQ_Contracts_ContractCode, 2026-08-18) a nivel de BD, pero se valida aquí antes
    /// para dar un mensaje claro en vez del 409 genérico de violación de índice.
    /// </summary>
    private async Task ValidateContractCodeUniqueAsync(int contractId, string contractCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(contractCode)) return;

        var duplicateId = await _db.Contracts
            .Where(c => c.ContractCode == contractCode && c.ContractID != contractId)
            .Select(c => (int?)c.ContractID)
            .FirstOrDefaultAsync(ct);

        if (duplicateId.HasValue)
            throw new BusinessRuleException(
                $"El código de contrato '{contractCode}' ya está en uso por el contrato #{duplicateId.Value}. Verifique el número de documento.");
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

        // 2026-07-06: el frontend ya oculta el botón de generar/regenerar a partir de
        // FIRMADO_CARGADO, pero el backend no lo exigía — ForceRegenerate=true podía
        // saltarse IsDocumentFrozen y sobreescribir el documento de un contrato ya
        // firmado/vigente. Este bloqueo es incondicional (no se salta con ForceRegenerate).
        var statusName = await _db.RefTypes
            .AsNoTracking()
            .Where(x => x.TypeId == contract.Status && x.Category == ContractStatusCategory)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct);

        if (statusName is not null && statusName != StatusBorrador && statusName != StatusGenerado)
            throw new InvalidOperationException(
                $"No se puede generar/regenerar el documento en estado '{statusName}' — el contrato ya fue firmado y cargado.");

        if (contract.IsDocumentFrozen && !request.ForceRegenerate)
            throw new InvalidOperationException(
                "El documento del contrato está congelado. Use ForceRegenerate=true para regenerarlo.");

        var employeeId = await _db.Employees
            .AsNoTracking()
            .Where(e => e.PersonID == contract.PersonID)
            .Select(e => (int?)e.EmployeeId)
            .FirstOrDefaultAsync(ct);

        // Si el contrato ya tiene un documento generado, reutilizar exactamente la misma versión
        // de plantilla usada originalmente (aunque ahora esté Archived), para no alterar el
        // contenido legal de un documento ya emitido al publicarse una nueva versión.
        var templateId = contract.GeneratedDocumentId.HasValue
            ? await _db.Set<GeneratedDocument>()
                .AsNoTracking()
                .Where(d => d.DocumentId == contract.GeneratedDocumentId.Value)
                .Select(d => (int?)d.TemplateId)
                .FirstOrDefaultAsync(ct)
              ?? await ResolveContractTemplateIdAsync(contract.ContractTypeID, contract.IsDelegation, ct)
            : await ResolveContractTemplateIdAsync(contract.ContractTypeID, contract.IsDelegation, ct);

        // Resolver responsables del contrato para variables de plantilla
        var (directorName, directorTitle) = await ResolveEmployeeInfoAsync(contract.DthDirectorId, ct);
        var (registrarName, _)            = await ResolveEmployeeInfoAsync(contract.CreatedBy, ct);

        var mergedOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static void SetOv(Dictionary<string, string> d, string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) d[key] = value!;
        }
        SetOv(mergedOverrides, "DTH_DIRECTOR_NAME",     directorName);
        SetOv(mergedOverrides, "DTH_DIRECTOR_FULLNAME", directorName);
        SetOv(mergedOverrides, "DTH_DIRECTOR_TITLE",    directorTitle);
        SetOv(mergedOverrides, "REGISTRAR_NAME",        registrarName);

        // Número del contrato, autogenerado al crear (ContractType.NumberingPrefix/Year/LastSequence).
        // CONTRACT_NUMBER está marcado como Manual en la plantilla CONTRATO_PROFESOR_OCASIONAL,
        // así que el resolver automático no lo toca salvo que se pase aquí como override.
        SetOv(mergedOverrides, "CONTRACT_NUMBER", contract.ContractCode);

        // Rol que desempeñará el contratado según el texto del contrato (ej. "Profesor Ocasional",
        // "Técnico Docente"); EMPLOYEE_CONTRACT_ROLE está marcado como Manual en las plantillas,
        // así que se resuelve aquí como override en vez de depender del resolver automático.
        var contractTypeForRole = await _db.ContractType
            .AsNoTracking()
            .FirstOrDefaultAsync(ct2 => ct2.ContractTypeId == contract.ContractTypeID, ct);
        SetOv(mergedOverrides, "EMPLOYEE_CONTRACT_ROLE", contractTypeForRole?.Name);

        // Fecha de suscripción en palabras (autorización del contrato, o hoy si aún no se autoriza).
        // CONTRATO_PROFESOR_OCASIONAL usa CONTRACT_DATE_DAY_WORDS/CONTRACT_DATE_MONTH/CONTRACT_DATE_YEAR_WORDS
        // en vez de DATE_DAY_WORDS/DATE_MONTH_NAME/DATE_YEAR_WORDS; se escriben ambos nombres.
        var subscriptionDate = contract.AuthorizationDate ?? DateTime.Now;
        var dayWords   = SpanishTextHelper.DayToWords(subscriptionDate);
        var monthName  = SpanishTextHelper.MonthName(subscriptionDate);
        var yearWords  = SpanishTextHelper.YearToWords(subscriptionDate);
        SetOv(mergedOverrides, "DATE_DAY_WORDS",          dayWords);
        SetOv(mergedOverrides, "DATE_MONTH_NAME",         monthName);
        SetOv(mergedOverrides, "DATE_YEAR_WORDS",         yearWords);
        SetOv(mergedOverrides, "CONTRACT_DATE_DAY_WORDS", dayWords);
        SetOv(mergedOverrides, "CONTRACT_DATE_MONTH",     monthName);
        SetOv(mergedOverrides, "CONTRACT_DATE_YEAR_WORDS", yearWords);

        // CONTRATO_PROFESOR_OCASIONAL usa DTH_REGISTRY_DATE_LONG (formato largo en palabras) en vez
        // de DTH_REGISTRY_DATE (formato corto); se escriben ambos.
        var registryNow = DateTime.Now;
        SetOv(mergedOverrides, "DTH_REGISTRY_DATE", SpanishTextHelper.ShortDate(registryNow));
        SetOv(mergedOverrides, "DTH_REGISTRY_DATE_LONG",
            $"{registryNow.Day} de {SpanishTextHelper.MonthName(registryNow)} de {registryNow.Year}");

        // ELABORATOR_FULLNAME (CONTRATO_PROFESOR_OCASIONAL) representa a quien elaboró este
        // contrato específico — se usa el mismo valor dinámico que REGISTRAR_NAME (creador del
        // registro) en vez de depender únicamente del nombre estático de InstitutionalConfig.
        SetOv(mergedOverrides, "ELABORATOR_FULLNAME", registrarName);

        // Remuneración y partida presupuestaria, desde la certificación financiera del contrato
        if (contract.CertificationID.HasValue)
        {
            var certification = await _db.Set<FinancialCertification>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CertificationId == contract.CertificationID.Value, ct);

            if (certification is not null)
            {
                var salary = certification.RmuCon ?? certification.RmuHour ?? 0;
                SetOv(mergedOverrides, "SALARY_WORDS",  SpanishTextHelper.AmountToWords(salary));
                SetOv(mergedOverrides, "SALARY_AMOUNT", salary.ToString("N2"));
                SetOv(mergedOverrides, "BUDGET_ITEM",   certification.Budget);
                // ACCION_PERSONAL y CONTRATO_PROFESOR_OCASIONAL usan BUDGET_CODE en vez de
                // BUDGET_ITEM; se escriben ambos nombres (mismo patrón que el resto de campos
                // con nomenclatura divergente entre plantillas).
                SetOv(mergedOverrides, "BUDGET_CODE",   certification.Budget);
            }
        }

        // Números/fechas de referencia de documentos institucionales (Resolución CAU, Memorando
        // de Rectorado, Resolución de Delegación): se toman del documento más reciente cargado
        // para este contrato con el DocumentTypeId correspondiente, resuelto por NOMBRE contra
        // HR.ref_Types (nunca un TypeId fijo, ver comentario en la declaración de las constantes),
        // en vez de pedírselos de nuevo al usuario si ya los adjuntó en documentos del contrato.
        var documentTypeIdsByName = await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.Category == "DOCUMENT_TYPE" && r.IsActive &&
                        (r.Name == DocumentTypeNameResolucionCau ||
                         r.Name == DocumentTypeNameMemorandoRectorado ||
                         r.Name == DocumentTypeNameResolucionDelegacion))
            .ToDictionaryAsync(r => r.Name, r => r.TypeId, ct);

        documentTypeIdsByName.TryGetValue(DocumentTypeNameResolucionCau, out var docTypeIdCau);
        documentTypeIdsByName.TryGetValue(DocumentTypeNameMemorandoRectorado, out var docTypeIdMemo);
        documentTypeIdsByName.TryGetValue(DocumentTypeNameResolucionDelegacion, out var docTypeIdDelegacion);

        var cauResolution = docTypeIdCau == 0 ? null : await _db.StoredFiles
            .AsNoTracking()
            .Where(f => f.EntityType == ContractEntityType && f.EntityId == contract.ContractID.ToString()
                     && f.DocumentTypeId == docTypeIdCau && f.Status == 1)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (cauResolution is not null)
        {
            SetOv(mergedOverrides, "CAU_RESOLUTION_NUMBER", cauResolution.DocumentReferenceNumber);
            SetOv(mergedOverrides, "CAU_RESOLUTION_DATE",
                cauResolution.DocumentReferenceDate.HasValue
                    ? SpanishTextHelper.ShortDate(cauResolution.DocumentReferenceDate.Value.ToDateTime(TimeOnly.MinValue))
                    : null);
        }

        var rectorMemo = docTypeIdMemo == 0 ? null : await _db.StoredFiles
            .AsNoTracking()
            .Where(f => f.EntityType == ContractEntityType && f.EntityId == contract.ContractID.ToString()
                     && f.DocumentTypeId == docTypeIdMemo && f.Status == 1)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (rectorMemo is not null)
        {
            var memoDate = rectorMemo.DocumentReferenceDate.HasValue
                ? SpanishTextHelper.ShortDate(rectorMemo.DocumentReferenceDate.Value.ToDateTime(TimeOnly.MinValue))
                : null;

            // CONTRATO_PROFESOR_OCASIONAL usa RECTOR_MEMO_*; las plantillas CONTRATO_TECNICO_* usan MEMORANDUM_*.
            SetOv(mergedOverrides, "RECTOR_MEMO_NUMBER", rectorMemo.DocumentReferenceNumber);
            SetOv(mergedOverrides, "RECTOR_MEMO_DATE",   memoDate);
            SetOv(mergedOverrides, "MEMORANDUM_NUMBER",  rectorMemo.DocumentReferenceNumber);
            SetOv(mergedOverrides, "MEMORANDUM_DATE",    memoDate);
        }

        // Si el contrato se firma por delegación, sobrescribir la autoridad firmante (Decano)
        // y los datos de la delegación, resueltos dinámicamente desde HR.tbl_DepartmentAuthorities
        // en vez de hardcodear nombres/resoluciones.
        if (contract.IsDelegation && contract.AuthorityNominatorId.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var delegateAuthority = await _db.DepartmentAuthorities
                .AsNoTracking()
                .Include(a => a.Employee).ThenInclude(e => e!.People)
                .Include(a => a.Department)
                .Include(a => a.AuthorityType)
                .Where(a => a.EmployeeId == contract.AuthorityNominatorId.Value
                         && a.IsActive
                         && a.StartDate <= today
                         && (a.EndDate == null || a.EndDate >= today))
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefaultAsync(ct);

            if (delegateAuthority is not null)
            {
                var person = delegateAuthority.Employee?.People;
                var fullName = person is null ? null : $"{person.FirstName} {person.LastName}".Trim();

                // Cargo real del delegado (Decano/Director/etc.) desde el catálogo AUTHORITY_TYPE,
                // en vez de un fallback hardcodeado a "Decano".
                var authorityRoleName = delegateAuthority.AuthorityType?.Name ?? delegateAuthority.Denomination;

                // Nombre de la Facultad: si el registro de autoridad quedó asociado a un departamento
                // que no es de tipo FACULTAD (ej. una Carrera), se sube por ParentId hasta encontrarla.
                var facultyName = await ResolveFacultyNameAsync(delegateAuthority.Department, ct);

                // Algunas plantillas usan AUTHORITY_NAME/AUTHORITY_ID (TÉCNICO_*) y otras
                // AUTHORITY_FULLNAME/AUTHORITY_IDCARD (PROFESOR_OCASIONAL); se escriben ambos
                // nombres para no romper ninguna de las dos convenciones existentes.
                SetOv(mergedOverrides, "AUTHORITY_NAME",        fullName);
                SetOv(mergedOverrides, "AUTHORITY_FULLNAME",    fullName);
                SetOv(mergedOverrides, "AUTHORITY_TITLE",       delegateAuthority.Denomination ?? authorityRoleName);
                SetOv(mergedOverrides, "AUTHORITY_ID",          person?.IdCard);
                SetOv(mergedOverrides, "AUTHORITY_IDCARD",      person?.IdCard);
                SetOv(mergedOverrides, "FACULTY_ROLE",          authorityRoleName);
                SetOv(mergedOverrides, "AUTHORITY_ROLE",        authorityRoleName);
                SetOv(mergedOverrides, "FACULTY_NAME",          facultyName ?? delegateAuthority.Department?.Name);
                SetOv(mergedOverrides, "DELEGATION_RESOLUTION", delegateAuthority.ResolutionCode);
                SetOv(mergedOverrides, "DELEGATION_DATE",       SpanishTextHelper.ShortDate(delegateAuthority.StartDate.ToDateTime(TimeOnly.MinValue)));
            }

            // Si además se adjuntó la resolución/acta de delegación específica para este contrato
            // (HR.tbl_StoredFile con DocumentTypeId=RESOLUCION_DELEGACION), su número/fecha de
            // referencia prevalece sobre el dato genérico de HR.tbl_DepartmentAuthorities, porque
            // es el documento concreto que respalda esta delegación puntual.
            var delegationResolutionDoc = docTypeIdDelegacion == 0 ? null : await _db.StoredFiles
                .AsNoTracking()
                .Where(f => f.EntityType == ContractEntityType && f.EntityId == contract.ContractID.ToString()
                         && f.DocumentTypeId == docTypeIdDelegacion && f.Status == 1)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (delegationResolutionDoc is not null)
            {
                SetOv(mergedOverrides, "DELEGATION_RESOLUTION", delegationResolutionDoc.DocumentReferenceNumber);
                SetOv(mergedOverrides, "DELEGATION_DATE",
                    delegationResolutionDoc.DocumentReferenceDate.HasValue
                        ? SpanishTextHelper.ShortDate(delegationResolutionDoc.DocumentReferenceDate.Value.ToDateTime(TimeOnly.MinValue))
                        : null);
            }
        }

        // Los overrides manuales del request tienen máxima prioridad
        if (request.Overrides is not null)
            foreach (var kvp in request.Overrides)
                mergedOverrides[kvp.Key] = kvp.Value;

        // Checklist de documentos obligatorios (HR.tbl_TramiteRequirements) para el módulo
        // CONTRACTS + este tipo de contrato específico. No bloquea si no hay nada configurado
        // (comportamiento actual sin cambios); lanza InvalidOperationException con el detalle
        // de lo faltante si algún requisito obligatorio no tiene documento cargado.
        var contractsModuleTypeId = await _db.RefTypes
            .AsNoTracking()
            .Where(r => r.Category == "ACCESS_MODULE_TYPE" && r.Name == ModuleTypeNameContracts && r.IsActive)
            .Select(r => (int?)r.TypeId)
            .FirstOrDefaultAsync(ct);

        if (contractsModuleTypeId.HasValue)
        {
            await _tramiteRequirements.ValidateRequiredDocumentsAsync(
                contractsModuleTypeId.Value, contract.ContractTypeID, ContractEntityType, contract.ContractID.ToString(), ct);
        }

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

        // El contrato ya quedó FIRMADO_CARGADO (uno de los dos estados que justifican
        // registrar el sueldo en SalaryHistory, junto con VIGENTE) — se registra aquí,
        // una sola vez, sin importar si más abajo continúa a VIGENTE o a FINALIZADO.
        await RecordSalaryHistoryForContractAsync(contract, "Contrato firmado y cargado.", ct);

        // 2026-08-06: "Ingresar Histórico" — el paso de subir el documento firmado de un
        // contrato histórico NO ocurre en la misma sesión que su creación (a diferencia de
        // Acciones de Personal), así que un flag transitorio pasado solo en el momento de
        // crear no llegaría de forma confiable hasta aquí. En su lugar, se deriva de la
        // propia fecha del contrato: si su EndDate ya pasó, por definición ya concluyó — no
        // debe anular a su padre, aprovisionar AD, ni quedar marcado VIGENTE. Esto también
        // cubre, de forma correcta, un contrato normal cuyo papeleo se cargó tarde y para
        // entonces ya venció.
        var alreadyEnded = contract.EndDate < DateTime.Today;

        // 2026-07-06: un adendum firmado y cargado anula automáticamente a su
        // contrato padre — la cadena de vigencia pasa siempre al adendum más
        // reciente. Antes de este control, un contrato y su adendum podían
        // quedar "vigentes" simultáneamente sin ninguna validación.
        if (contract.ParentID.HasValue && !alreadyEnded)
            await AnnulParentContractAsync(contract.ParentID.Value, contractId, updatedBy, ct);

        // Disparar aprovisionamiento AD si el tipo de contrato lo requiere (solo contratos raíz)
        if (contract.ParentID is null && !alreadyEnded)
        {
            var contractType = await _db.ContractType
                .AsNoTracking()
                .Where(x => x.ContractTypeId == contract.ContractTypeID)
                .Select(x => new { x.RequiresAdUserCreation })
                .FirstOrDefaultAsync(ct);

            if (contractType?.RequiresAdUserCreation == true)
                await TriggerProvisioningAsync(contract.PersonID, contract.DepartmentID, contractId, updatedBy, ct);
        }

        if (alreadyEnded)
        {
            _logger.LogInformation(
                "ContractsService: contrato {ContractId} ya venció (EndDate={EndDate}) al momento de cargar el documento firmado — cerrado directo a FINALIZADO sin pasar por VIGENTE, sin anular padre ni aprovisionar AD.",
                contractId, contract.EndDate);

            await UpdateContractDocumentStatusAsync(
                contract, StatusFinalizado,
                "Contrato ya concluido (fecha de fin anterior a hoy) al cargar el documento firmado.",
                updatedBy, ct);
            return;
        }

        // 2026-07-06: FIRMADO_CARGADO + documento cargado siempre pasa a VIGENTE,
        // para contratos raíz Y adendums. Antes, VIGENTE solo se disparaba si el
        // aprovisionamiento AD tenía éxito — un contrato cuyo tipo no requería AD
        // (RequiresAdUserCreation=false), o cuyo aprovisionamiento fallaba, se
        // quedaba en FIRMADO_CARGADO para siempre (confirmado: 0 contratos en
        // VIGENTE en producción). El aprovisionamiento AD es un efecto secundario
        // independiente de que el contrato esté vigente, no un prerequisito.
        await UpdateContractDocumentStatusAsync(
            contract, StatusVigente,
            "Contrato vigente tras firma y carga de documento.",
            updatedBy, ct);
    }

    /// <summary>
    /// Registra/actualiza en <c>HR.tbl_SalaryHistory</c> el sueldo del contrato indicado
    /// (documento fuente = este <see cref="Contracts.ContractID"/>). No hace nada si el
    /// contrato no tiene <see cref="Contracts.BaseSalary"/> cargado — el disparo automático
    /// (firma) y la corrección manual comparten esta misma lógica de upsert.
    /// </summary>
    private async Task RecordSalaryHistoryForContractAsync(Contracts contract, string reason, CancellationToken ct)
    {
        if (!contract.BaseSalary.HasValue)
            return;

        var employeeId = await _db.Employees
            .AsNoTracking()
            .Where(e => e.PersonID == contract.PersonID && e.IsActive)
            .Select(e => e.EmployeeId)
            .FirstOrDefaultAsync(ct);

        if (employeeId == 0)
        {
            _logger.LogWarning(
                "ContractsService: no se encontró empleado activo para PersonID={PersonID} (ContractID={ContractID}) — no se registra SalaryHistory.",
                contract.PersonID, contract.ContractID);
            return;
        }

        await _salaryHistory.UpsertForContractAsync(
            contract.ContractID, employeeId, contract.BaseSalary.Value,
            _currentUser.UserName ?? _currentUser.Email ?? "system", reason, ct);
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

    private static readonly string[] ClosedContractStatuses =
        [StatusAnulado, StatusFinalizado, "VENCIDO", "RENUNCIA"];

    /// <summary>
    /// Anula el contrato padre cuando su adendum queda formalizado (firmado y
    /// cargado). No pisa un estado terminal más específico que el padre ya
    /// pudiera tener (ej. FINALIZADO, VENCIDO), y es un no-op si ya está anulado
    /// (cadena de varios adendums sucesivos).
    /// </summary>
    private async Task AnnulParentContractAsync(int parentContractId, int addendumContractId, int updatedBy, CancellationToken ct)
    {
        var parent = await _db.Set<Contracts>().FirstOrDefaultAsync(x => x.ContractID == parentContractId, ct);
        if (parent is null)
            return;

        var parentStatusName = await _db.RefTypes
            .AsNoTracking()
            .Where(x => x.TypeId == parent.Status && x.Category == ContractStatusCategory)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct);

        if (parentStatusName is not null && ClosedContractStatuses.Contains(parentStatusName))
            return;

        var anuladoId = await GetContractStatusIdAsync(StatusAnulado, ct);

        parent.Status = anuladoId;
        parent.UpdatedAt = DateTime.Now;
        parent.UpdatedBy = updatedBy > 0 ? updatedBy : null;

        _db.ContractStatusHistories.Add(new ContractStatusHistory
        {
            ContractID   = parentContractId,
            StatusTypeID = anuladoId,
            Comment      = $"Anulado automáticamente por adendum ContractID={addendumContractId}.",
            ChangedBy    = updatedBy > 0 ? updatedBy : _currentUser.EmployeeId,
            ChangedAt    = DateTime.Now,
        });

        await _db.SaveChangesAsync(ct);
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
        bool isDelegation,
        CancellationToken ct)
    {
        var contractType = await _db.ContractType
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContractTypeId == contractTypeId, ct)
            ?? throw new KeyNotFoundException($"Tipo de contrato id={contractTypeId} no existe.");

        // Si el contrato es por delegación y existe una plantilla de delegación vinculada, usarla;
        // en cualquier otro caso (sin delegación, o sin plantilla de delegación configurada), usar la plantilla por defecto.
        var preferredTemplateId = isDelegation && contractType.DelegationTemplateId.HasValue
            ? contractType.DelegationTemplateId
            : contractType.DefaultTemplateId;

        if (preferredTemplateId.HasValue)
        {
            var template = await _templateRepository.GetByIdAsync(
                preferredTemplateId.Value,
                ct)
                ?? throw new KeyNotFoundException(
                    $"La plantilla {preferredTemplateId.Value} no existe.");

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
                person.LastName,
                person.FirstName,
                person.PreferredDenomination,
                JobTitle = (string?)job.Description
            }
        ).FirstOrDefaultAsync(ct);

        if (row is null) return (null, null);

        // Nombre a imprimir: PreferredDenomination (nombre/título formal autoeditado) si
        // existe, si no la concatenación LastName+FirstName de siempre. Mismo criterio
        // que PersonnelActionRepository.ResolveEmployeeAsync — solo aplica a firmas de
        // documentos (Acciones de Personal / Contratos), no es un cambio global de cómo
        // se muestra el nombre en el resto del sistema.
        var name = !string.IsNullOrWhiteSpace(row.PreferredDenomination)
            ? row.PreferredDenomination.Trim()
            : $"{row.LastName} {row.FirstName}".Trim();

        // Si la persona seleccionada tiene una autoridad institucional vigente (Rector,
        // Vicerrector, Decano, etc.), su denominación de autoridad prevalece sobre el
        // cargo genérico de HR.tbl_Employees.JobId.
        var today = DateOnly.FromDateTime(DateTime.Now);
        var authorityDenomination = await _db.DepartmentAuthorities.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId.Value
                && a.IsActive
                && a.StartDate <= today
                && (a.EndDate == null || a.EndDate >= today))
            .OrderByDescending(a => a.StartDate)
            .Select(a => a.Denomination)
            .FirstOrDefaultAsync(ct);

        var jobTitle = !string.IsNullOrWhiteSpace(authorityDenomination) ? authorityDenomination : row.JobTitle;

        return (name, jobTitle);
    }

    /// <summary>
    /// Resuelve el nombre de la Facultad real a partir del departamento de una autoridad delegada,
    /// subiendo por ParentId si el registro quedó asociado a un departamento de otro tipo (ej. Carrera).
    /// Limita la subida a 5 niveles para evitar bucles ante datos jerárquicos corruptos.
    /// </summary>
    private async Task<string?> ResolveFacultyNameAsync(Departments? department, CancellationToken ct)
    {
        var current = department;
        for (var i = 0; i < 5 && current is not null; i++)
        {
            if (current.DepartmentType == DepartmentTypeIdFacultad)
                return current.Name;

            if (!current.ParentId.HasValue) break;

            current = await _db.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DepartmentId == current!.ParentId.Value, ct);
        }

        return null;
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
            join owner in _db.Employees.AsNoTracking()     on c.PersonID        equals owner.PersonID into ownerg
            from owner in ownerg.DefaultIfEmpty()
            where (!filter.StartDate.HasValue         || c.StartDate >= filter.StartDate.Value)
               && (!filter.EndDate.HasValue           || c.StartDate <= filter.EndDate.Value)
               && (!filter.DepartmentId.HasValue      || c.DepartmentID    == filter.DepartmentId.Value)
               && (!filter.ContractTypeId.HasValue    || c.ContractTypeID  == filter.ContractTypeId.Value)
               && (!filter.LaborRegimeId.HasValue     || c.LaborRegimeID   == filter.LaborRegimeId.Value)
               && (!filter.CreatedByEmployeeId.HasValue || c.CreatedBy     == filter.CreatedByEmployeeId.Value)
               && (!filter.EmployeeId.HasValue        || (owner != null && owner.EmployeeId == filter.EmployeeId.Value))
               && (string.IsNullOrEmpty(filter.Status) || (rs != null && rs.Name == filter.Status))
            orderby p.LastName, p.FirstName, c.StartDate descending
            select new ContractReportDto
            {
                ContractId        = c.ContractID,
                ContractCode      = c.ContractCode,
                PersonIdCard      = p.IdCard,
                PersonFullName    = p.LastName + " " + p.FirstName,
                DepartmentName    = d.Name,
                ContractTypeName  = ct2.Name,
                LaborRegimeName   = lr != null ? lr.Name : null,
                WorkModalityName  = wm != null ? wm.Name : null,
                ContractedHours   = c.ContractedHours,
                JobTitle          = j != null ? j.Description : null,
                CreatedByName     = cbp != null ? cbp.LastName + " " + cbp.FirstName : null,
                StartDate         = c.StartDate,
                EndDate           = c.EndDate,
                StatusName        = rs != null ? rs.Name : "—"
            };

        return await query.ToListAsync(ct);
    }
}

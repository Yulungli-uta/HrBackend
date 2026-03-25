// File: Application/Services/DocflowService.cs
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Docflow;
using WsUtaSystem.Application.DTOs.Documents;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Application.Services
{
    public sealed class DocflowService : IDocflowService
    {
        #region 1. Fields & Constructor

        private const string DirectoryCode = "DOCFLOW";
        private const string EntityType = "DOCFLOW_DOCUMENT";

        /// <summary>
        /// Opciones centralizadas de serialización JSON.
        /// CamelCase para consistencia con la BD; CaseInsensitive como red de
        /// seguridad para datos legacy ya almacenados.
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly IDocflowProcessRepository _procRepo;
        private readonly IDocflowInstanceRepository _instRepo;
        private readonly IDocflowDocumentRepository _docRepo;
        private readonly IDocflowMovementRepository _movRepo;
        private readonly IDepartmentsService _departmentsService;
        private readonly IDocFlowDocumentRuleRepository _docRuleRepo;
        private readonly IDocumentOrchestratorService _docOrchestrator;
        private readonly ICurrentUserService _currentUser;
        private readonly IMapper _mapper;
        private readonly ILogger<DocflowService> _logger;

        public DocflowService(
            IDocflowProcessRepository procRepo,
            IDocflowInstanceRepository instRepo,
            IDocflowDocumentRepository docRepo,
            IDocflowMovementRepository movRepo,
            IDepartmentsService departmentsService,
            IDocFlowDocumentRuleRepository docRuleRepo,
            IDocumentOrchestratorService docOrchestrator,
            ICurrentUserService currentUser,
            IMapper mapper,
            ILogger<DocflowService> logger)
        {
            _procRepo = procRepo ?? throw new ArgumentNullException(nameof(procRepo));
            _instRepo = instRepo ?? throw new ArgumentNullException(nameof(instRepo));
            _docRepo = docRepo ?? throw new ArgumentNullException(nameof(docRepo));
            _movRepo = movRepo ?? throw new ArgumentNullException(nameof(movRepo));
            _departmentsService = departmentsService ?? throw new ArgumentNullException(nameof(departmentsService));
            _docRuleRepo = docRuleRepo ?? throw new ArgumentNullException(nameof(docRuleRepo));
            _docOrchestrator = docOrchestrator ?? throw new ArgumentNullException(nameof(docOrchestrator));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region 2. Configuración de Procesos (CRUD & Query)

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProcessDto>> GetProcessesAsync(CancellationToken ct)
        {
            var items = await _procRepo.GetAllActiveAsync(ct);
            return _mapper.Map<List<ProcessDto>>(items);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<ProcessDto>> GetProcessByIdAsync(int processId, CancellationToken ct)
        {
            var items = await _procRepo.GetByIdAsync(processId, ct);
            return _mapper.Map<List<ProcessDto>>(items);
        }

        /// <inheritdoc/>
        public async Task<ProcessDto> CreateProcessAsync(CreateProcessRequestDto req, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(req);

            _logger.LogInformation("Creando nuevo proceso: {ProcessName}", req.ProcessName);

            if (req.ResponsibleDepartmentId is int deptId && deptId > 0)
            {
                _ = await _departmentsService.GetByIdAsync(deptId, ct)
                    ?? throw new KeyNotFoundException($"Departamento {deptId} no encontrado.");
            }

            if (req.ParentId.HasValue)
            {
                _ = await _procRepo.GetByIdAsync(req.ParentId.Value, ct)
                    ?? throw new KeyNotFoundException($"Proceso padre {req.ParentId} no encontrado.");
            }

            var process = new DocflowProcessHierarchy
            {
                ProcessCode = req.ProcessCode ?? GenerateProcessCode(req.ProcessName),
                ProcessName = req.ProcessName,
                ParentId = req.ParentId,
                ResponsibleDepartmentId = req.ResponsibleDepartmentId,
                IsActive = req.IsActive,
                ProcessFolderName = string.Empty,
                DynamicFieldMetadata = "[]"
            };

            await _procRepo.AddAsync(process, ct);
            return _mapper.Map<ProcessDto>(process);
        }

        /// <inheritdoc/>
        public async Task<ProcessDto> UpdateProcessAsync(int processId, UpdateProcessRequestDto req, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(req);

            var process = await _procRepo.GetByIdAsync(processId, ct)
                ?? throw new KeyNotFoundException($"Proceso {processId} no encontrado.");

            if (req.ResponsibleDepartmentId.HasValue)
                process.ResponsibleDepartmentId = req.ResponsibleDepartmentId.Value;

            if (!string.IsNullOrWhiteSpace(req.ProcessName))
                process.ProcessName = req.ProcessName;

            if (req.ParentId.HasValue)
                process.ParentId = req.ParentId;

            if (req.IsActive.HasValue)
                process.IsActive = req.IsActive.Value;

            await _procRepo.UpdateAsync(processId, process, ct);
            return _mapper.Map<ProcessDto>(process);
        }

        /// <inheritdoc/>
        /// <remarks>Implementa soft-delete: marca IsActive = false.</remarks>
        public async Task DeleteProcessAsync(int processId, CancellationToken ct)
        {
            var process = await _procRepo.GetByIdAsync(processId, ct)
                ?? throw new KeyNotFoundException($"Proceso {processId} no encontrado.");

            process.IsActive = false;
            await _procRepo.UpdateAsync(processId, process, ct);

            _logger.LogInformation("Proceso {ProcessId} desactivado (soft-delete).", processId);
        }

        #endregion

        #region 3. Metadatos y Campos Dinámicos

        /// <summary>
        /// Obtiene los campos dinámicos (metadata) de un proceso.
        /// Usa <see cref="_jsonOptions"/> para garantizar consistencia
        /// con la serialización camelCase que se persiste en BD.
        /// </summary>
        public async Task<ProcessDynamicFieldsDto> GetProcessDynamicFieldsAsync(int processId, CancellationToken ct)
        {
            _logger.LogInformation("Obteniendo campos dinámicos del proceso: {ProcessId}", processId);

            _ = await _procRepo.GetByIdAsync(processId, ct)
                ?? throw new KeyNotFoundException($"Proceso {processId} no encontrado.");

            var json = await _procRepo.GetDynamicFieldMetadataJsonAsync(processId, ct);

            // Normalizar JSON vacío o nulo
            if (string.IsNullOrWhiteSpace(json))
                json = "[]";

            // Una sola deserialización con opciones correctas (camelCase + caseInsensitive)
            List<DynamicFieldSchemaDto>? fields = null;
            try
            {
                fields = JsonSerializer.Deserialize<List<DynamicFieldSchemaDto>>(json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex,
                    "JSON inválido almacenado para proceso {ProcessId}. Se retorna lista vacía.", processId);
            }

            _logger.LogInformation(
                "Campos dinámicos obtenidos para proceso {ProcessId}. Total: {Count}",
                processId, fields?.Count ?? 0);

            return new ProcessDynamicFieldsDto
            {
                ProcessId = processId,
                DynamicFieldMetadata = fields ?? []
            };
        }

        /// <summary>
        /// Actualiza los campos dinámicos (metadata) de un proceso.
        /// Serializa con <see cref="_jsonOptions"/> (camelCase) para
        /// mantener consistencia con <see cref="GetProcessDynamicFieldsAsync"/>.
        /// </summary>
        public async Task UpdateProcessDynamicFieldsAsync(int processId, UpdateProcessDynamicFieldsRequest req, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(req);

            _logger.LogInformation("Actualizando campos dinámicos del proceso: {ProcessId}", processId);

            _ = await _procRepo.GetByIdAsync(processId, ct)
                ?? throw new KeyNotFoundException($"Proceso {processId} no encontrado.");

            var validator = new UpdateProcessDynamicFieldsRequestValidator();
            var vr = await validator.ValidateAsync(req, ct);
            if (!vr.IsValid)
            {
                var errors = string.Join("; ", vr.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning(
                    "Validación fallida para campos dinámicos del proceso {ProcessId}: {Errors}",
                    processId, errors);
                throw new ValidationException(vr.Errors);
            }

            // Serialización con las mismas opciones que se usan al leer → simetría garantizada
            var json = JsonSerializer.Serialize(req.DynamicFieldMetadata, _jsonOptions);

            await _procRepo.UpdateDynamicFieldMetadataJsonAsync(processId, json, ct);

            _logger.LogInformation(
                "Campos dinámicos actualizados para proceso {ProcessId}. Total campos: {Count}",
                processId, req.DynamicFieldMetadata.Count);
        }

        #endregion

        #region 4. Reglas de Documentación (Rules)

        /// <summary>Obtiene todas las reglas de documentos disponibles.</summary>
        public async Task<IReadOnlyList<DocumentRuleDto>> GetRuleAsync(CancellationToken ct)
        {
            _logger.LogInformation("Obteniendo todas las reglas de documentos");

            var rules = await _docRuleRepo.GetAllAsync(ct);
            return _mapper.Map<List<DocumentRuleDto>>(rules);
        }

        /// <summary>Obtiene todas las reglas asociadas a un proceso específico.</summary>
        public async Task<IReadOnlyList<DocumentRuleDto>> GetRulesByProcessAsync(int processId, CancellationToken ct)
        {
            _logger.LogInformation("Obteniendo reglas para proceso: {ProcessId}", processId);

            _ = await _procRepo.GetByIdAsync(processId, ct)
                ?? throw new KeyNotFoundException($"Proceso con ID {processId} no encontrado.");

            var rules = await _procRepo.GetRulesByProcessAsync(processId, ct);
            return _mapper.Map<List<DocumentRuleDto>>(rules);
        }

        /// <summary>Crea una nueva regla de documento para un proceso.</summary>
        public async Task<DocumentRuleDto> CreateRuleAsync(int processId, CreateDocumentRuleRequestDto req, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(req);

            _logger.LogInformation(
                "Creando nueva regla para proceso: {ProcessId}, tipo: {DocumentType}",
                processId, req.DocumentType);

            _ = await _procRepo.GetByIdAsync(processId, ct)
                ?? throw new KeyNotFoundException($"Proceso con ID {processId} no encontrado.");

            var existingRules = await _procRepo.GetRulesByProcessAsync(processId, ct);
            if (existingRules.Any(r => r.DocumentType == req.DocumentType))
                throw new InvalidOperationException(
                    $"Ya existe una regla para el tipo de documento '{req.DocumentType}' en este proceso.");

            var rule = new DocflowDocumentRule
            {
                ProcessId = processId,
                DocumentType = req.DocumentType,
                IsRequired = req.IsRequired,
                DefaultVisibility = (byte)req.DefaultVisibility,
                AllowVisibilityOverride = req.AllowVisibilityOverride
            };

            await _docRuleRepo.AddAsync(rule, ct);

            _logger.LogInformation(
                "Regla creada exitosamente para proceso: {ProcessId}, tipo: {DocumentType}",
                processId, req.DocumentType);

            return _mapper.Map<DocumentRuleDto>(rule);
        }

        /// <summary>Actualiza una regla de documento existente.</summary>
        public async Task<DocumentRuleDto> UpdateRuleAsync(int ruleId, UpdateDocumentRuleRequestDto req, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(req);

            _logger.LogInformation("Actualizando regla: {RuleId}", ruleId);

            var rule = await _docRuleRepo.GetByIdAsync(ruleId, ct)
                ?? throw new KeyNotFoundException($"Regla con ID {ruleId} no encontrada.");

            if (!string.IsNullOrWhiteSpace(req.DocumentType))
            {
                var existingRules = await _procRepo.GetRulesByProcessAsync(rule.ProcessId, ct);
                if (existingRules.Any(r => r.RuleId != ruleId && r.DocumentType == req.DocumentType))
                    throw new InvalidOperationException(
                        $"Ya existe otra regla para el tipo de documento '{req.DocumentType}' en este proceso.");

                rule.DocumentType = req.DocumentType;
            }

            if (req.IsRequired.HasValue)
                rule.IsRequired = req.IsRequired.Value;

            if (req.DefaultVisibility.HasValue)
                rule.DefaultVisibility = (byte)req.DefaultVisibility.Value;

            if (req.AllowVisibilityOverride.HasValue)
                rule.AllowVisibilityOverride = req.AllowVisibilityOverride.Value;

            await _docRuleRepo.UpdateAsync(rule.RuleId, rule, ct);

            _logger.LogInformation("Regla actualizada exitosamente: {RuleId}", ruleId);

            return _mapper.Map<DocumentRuleDto>(rule);
        }

        /// <summary>Elimina una regla de documento (hard-delete).</summary>
        public async Task DeleteRuleAsync(int ruleId, CancellationToken ct)
        {
            _logger.LogInformation("Eliminando regla: {RuleId}", ruleId);

            var rule = await _docRuleRepo.GetByIdAsync(ruleId, ct)
                ?? throw new KeyNotFoundException($"Regla con ID {ruleId} no encontrada.");

            var hasActiveInstances = await _instRepo.HasActiveInstancesAsync(rule.ProcessId, ct);
            if (hasActiveInstances)
                throw new InvalidOperationException(
                    "No se puede eliminar la regla porque existen expedientes activos usando este proceso.");

            await _docRuleRepo.DeleteAsync(rule, ct);

            _logger.LogInformation("Regla eliminada exitosamente: {RuleId}", ruleId);
        }

        #endregion

        #region 5. Gestión de Expedientes (Instances)

        /// <inheritdoc/>
        public async Task<InstanceDetailDto> CreateInstanceAsync(CreateInstanceRequest req, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(req);

            _logger.LogInformation(
                "Creando nuevo expediente '{InstanceName}' para el proceso inicial: {InitialProcessId}",
                req.InstanceName ?? "SIN_NOMBRE", req.InitialProcessId);

            // 1. Validar que el proceso inicial existe
            var initialProcess = await _procRepo.GetByIdAsync(req.InitialProcessId, ct)
                ?? throw new KeyNotFoundException($"Proceso inicial {req.InitialProcessId} no encontrado.");

            // 2. Encontrar el Macroproceso Raíz
            var rootProcessId = await GetRootProcessIdAsync(initialProcess.ProcessId, ct);

            // 3. Validar que el proceso tiene un departamento responsable (CORRECCIÓN AQUÍ)
            if (initialProcess.ResponsibleDepartmentId <= 0)
            {
                throw new InvalidOperationException(
                    $"El proceso {req.InitialProcessId} no tiene un departamento responsable definido.");
            }

            var departmentId = initialProcess.ResponsibleDepartmentId;

            // 4. Obtener el ID del usuario actual
            var userId = GetEmployeeIdOrThrow();

            // 5. Crear la nueva instancia
            var instance = new DocflowWorkflowInstance
            {
                InstanceId = Guid.NewGuid(),
                ProcessId = req.InitialProcessId,
                RootProcessId = rootProcessId,                    // NUEVO CAMPO
                InstanceName = req.InstanceName,                  // NUEVO CAMPO
                CurrentStatus = "IN_PROGRESS",
                CurrentDepartmentId = departmentId,
                AssignedToUserId = req.AssignedToUserId,
                DynamicMetadata = req.DynamicMetadata ?? "{}",
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            // 6. Validar metadata dinámica contra el esquema del proceso
            await ValidateDynamicMetadataAgainstProcessSchemaAsync(initialProcess.ProcessId, req.DynamicMetadata, ct);

            // 7. Guardar la instancia
            await _instRepo.CreateAsync(instance, ct);

            _logger.LogInformation(
                "Expediente '{InstanceName}' ({InstanceId}) creado exitosamente para el macroproceso {RootProcessId}.",
                instance.InstanceName ?? "SIN_NOMBRE", instance.InstanceId, instance.RootProcessId);

            return _mapper.Map<InstanceDetailDto>(instance);
        }

        /// <inheritdoc/>
        public async Task<InstanceDetailDto> GetInstanceAsync(Guid instanceId, CancellationToken ct)
        {
            var deptId = await GetDepartmentIdOrThrowAsync(ct);
            var inst = await RequireInstanceReadAsync(instanceId, deptId, ct);
            return _mapper.Map<InstanceDetailDto>(inst);
        }

        /// <inheritdoc/>
        public async Task<PagedResultDto<InstanceListItemDto>> SearchInstancesAsync(
            int page,
            int pageSize,
            string? status,
            int? processId,
            string? q,
            DateTime? from,
            DateTime? to,
            CancellationToken ct)
        {
            if (page <= 0) page = 1;

            var deptId = await GetDepartmentIdOrThrowAsync(ct);
            var (items, total) = await _instRepo.SearchAsync(page, pageSize, status, processId, q, from, to, deptId, ct);

            return new PagedResultDto<InstanceListItemDto>
            {
                Items = _mapper.Map<List<InstanceListItemDto>>(items),
                Page = page,
                PageSize = pageSize,
                Total = total
            };
        }

        #endregion

        #region 6. Documentos y Versiones (Archivos)

        /// <inheritdoc/>
        public async Task<IReadOnlyList<DocumentDto>> GetInstanceDocumentsAsync(Guid instanceId, CancellationToken ct)
        {
            var deptId = await GetDepartmentIdOrThrowAsync(ct);
            _ = await RequireInstanceReadAsync(instanceId, deptId, ct);
            var docs = await _docRepo.GetVisibleByInstanceAsync(instanceId, deptId, ct);
            return _mapper.Map<List<DocumentDto>>(docs);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// TODO: Completar el mapeo manual de <see cref="DocflowDocument"/>
        /// con los campos requeridos por la entidad.
        /// </remarks>
        public async Task<DocumentDto> CreateDocumentAsync(Guid instanceId, CreateDocumentRequest req, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(req);

            var deptId = await GetDepartmentIdOrThrowAsync(ct);
            var inst = await RequireInstanceReadAsync(instanceId, deptId, ct);
            RequireInstanceWrite(inst, deptId);

            // TODO: Completar mapeo desde req → DocflowDocument (campos obligatorios de la entidad)
            throw new NotImplementedException(
                "CreateDocumentAsync: el mapeo de DocflowDocument aún no está implementado. " +
                "Completar según los campos requeridos por la entidad.");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<FileVersionDto>> GetDocumentVersionsAsync(Guid documentId, CancellationToken ct)
        {
            var deptId = await GetDepartmentIdOrThrowAsync(ct);

            var doc = await _docRepo.GetByIdAsync(documentId, ct)
                ?? throw new KeyNotFoundException($"Documento {documentId} no encontrado.");

            _ = await RequireInstanceReadAsync(doc.InstanceId, deptId, ct);

            var versions = await _docRepo.GetVersionsAsync(documentId, ct);
            return _mapper.Map<List<FileVersionDto>>(versions);
        }

        /// <inheritdoc/>
        public async Task<FileVersionDto> UploadDocumentVersionAsync(Guid documentId, IFormFile file, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(file);

            var deptId = await GetDepartmentIdOrThrowAsync(ct);

            var doc = await _docRepo.GetByIdAsync(documentId, ct)
                ?? throw new KeyNotFoundException($"Documento {documentId} no encontrado.");

            var inst = await _instRepo.GetByIdAsync(doc.InstanceId, ct)
                ?? throw new KeyNotFoundException($"Instancia {doc.InstanceId} no encontrada.");

            RequireInstanceWrite(inst, deptId);

            var relativePath = BuildRelativePath(string.Empty, inst.InstanceId, doc.DocumentId);
            var uploadReq = new DocumentUploadSingleRequestDto
            {
                File = file,
                RelativePath = relativePath,
                DirectoryCode = DirectoryCode
            };

            var result = await _docOrchestrator.UploadSingleAndRegisterAsync(uploadReq, ct);

            // TODO: Persistir la versión en la BD y retornar el DTO mapeado
            _logger.LogInformation(
                "Versión subida para documento {DocumentId}. Path: {Path}",
                documentId, relativePath);

            return new FileVersionDto(); // placeholder: reemplazar con entidad persistida
        }

        /// <inheritdoc/>
        public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadVersionAsync(Guid versionId, CancellationToken ct)
        {
            var version = await _docRepo.GetVersionByIdAsync(versionId, ct)
                ?? throw new KeyNotFoundException($"Versión {versionId} no encontrada.");

            if (!Guid.TryParse(version.StoragePath, out var storageGuid))
                throw new InvalidOperationException(
                    $"StoragePath '{version.StoragePath}' no es un GUID válido para la versión {versionId}.");

            var downloaded = await _docOrchestrator.DownloadByGuidAsync(storageGuid, ct)
                ?? throw new InvalidOperationException($"No se pudo descargar el archivo para la versión {versionId}.");

            var (fileBytes, contentType, fileName) = downloaded;
            return (fileBytes, contentType, fileName);
        }

        #endregion

        #region 7. Flujos, Movimientos y Auditoría

        /// <inheritdoc/>
        public async Task CreateMovementAsync(Guid instanceId, CreateMovementRequest req, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(req);
            // TODO: Implementar lógica de Forward/Return
            throw new NotImplementedException("CreateMovementAsync: lógica de movimiento aún no implementada.");
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<object>> GetReturnsAuditAsync(
            DateTime? from,
            DateTime? to,
            int? processId,
            int? userId,
            CancellationToken ct)
        {
            var rows = await _movRepo.GetReturnsAuditAsync(from, to, processId, userId, ct);

            return rows.Select(r => (object)new
            {
                r.MovementId,
                r.Comments,
                r.CreatedAt
            }).ToList();
        }

        //// <summary>
        /// Navega recursivamente hacia arriba en la jerarquía de procesos para encontrar el ID del proceso raíz.
        /// Si el proceso no tiene padre, es el raíz.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Si el proceso no existe.</exception>
        /// <exception cref="InvalidOperationException">Si hay inconsistencia en la jerarquía.</exception>
        private async Task<int> GetRootProcessIdAsync(int processId, CancellationToken ct)
        {
            var currentProcess = await _procRepo.GetByIdAsync(processId, ct)
                ?? throw new KeyNotFoundException($"Proceso {processId} no encontrado durante la búsqueda del raíz.");

            // Si el proceso no tiene padre, es el raíz.
            if (!currentProcess.ParentId.HasValue)
            {
                _logger.LogDebug("Proceso {ProcessId} es el raíz.", processId);
                return currentProcess.ProcessId;
            }

            // Si tiene padre, buscar recursivamente el padre del padre.
            // Se agrega un límite de seguridad para evitar bucles infinitos.
            int currentId = currentProcess.ParentId.Value;
            for (int i = 0; i < 20; i++) // Límite de 20 niveles de profundidad
            {
                var parent = await _procRepo.GetByIdAsync(currentId, ct);
                if (parent == null)
                {
                    throw new InvalidOperationException($"Inconsistencia de datos: Proceso padre {currentId} no encontrado.");
                }

                if (!parent.ParentId.HasValue)
                {
                    _logger.LogDebug("Proceso raíz encontrado: {RootProcessId} para el proceso inicial {ProcessId}.",
                        parent.ProcessId, processId);
                    return parent.ProcessId; // Encontramos el raíz
                }

                currentId = parent.ParentId.Value;
            }

            throw new InvalidOperationException("Se excedió el límite de jerarquía (20 niveles) buscando el proceso raíz.");
        }
        #endregion

        #region 8. Private Helpers

        /// <summary>Obtiene el EmployeeId del usuario autenticado o lanza <see cref="UnauthorizedAccessException"/>.</summary>
        private int GetEmployeeIdOrThrow()
            => _currentUser.EmployeeId
               ?? throw new UnauthorizedAccessException("El usuario actual no tiene un EmployeeId asociado.");

        /// <summary>
        /// Resuelve el DepartmentId del usuario actual desde el token JWT.
        /// Loguea el objeto completo de _currentUser en JSON para debugging.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">
        /// Se lanza si el usuario no tiene DepartmentID configurado en el token.
        /// </exception>
        private Task<int> GetDepartmentIdOrThrowAsync(CancellationToken ct)
        {
            try
            {
                // Log del objeto completo en JSON para debugging
                var currentUserJson = JsonSerializer.Serialize(_currentUser, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // CAMBIO: LogInformation en lugar de LogDebug
                _logger.LogInformation(
                    "Resolviendo DepartmentId para usuario. CurrentUser JSON: {CurrentUserJson}",
                    currentUserJson);

                // Validar que DepartmentID existe
                if (!_currentUser.DepartmentID.HasValue)
                {
                    _logger.LogError(
                        "DepartmentID es NULL para usuario {UserId}. " +
                        "Verifique que el token JWT contiene el claim 'departmentid'.",
                        _currentUser.EmployeeId);

                    throw new UnauthorizedAccessException(
                        "El departamento del usuario actual no pudo resolverse. " +
                        "Verifique que el token JWT contiene el claim 'departmentid'.");
                }

                var departmentId = _currentUser.DepartmentID.Value;

                _logger.LogInformation(
                    "DepartmentId resuelto exitosamente para usuario {UserId}: {DepartmentId}",
                    _currentUser.EmployeeId, departmentId);

                return Task.FromResult(departmentId);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error inesperado al resolver DepartmentId para usuario {UserId}",
                    _currentUser.UserName ?? "UNKNOWN");
                throw;
            }
        }

        /// <summary>
        /// Genera un código de proceso a partir del nombre.
        /// Toma los primeros 3 caracteres (uppercase) + los últimos 10 dígitos del timestamp.
        /// </summary>
        private static string GenerateProcessCode(string name)
        {
            var prefix = name[..Math.Min(3, name.Length)].ToUpperInvariant();
            var ticks = DateTime.Now.Ticks.ToString();
            var suffix = ticks.Length >= 10 ? ticks[^10..] : ticks;
            return $"{prefix}{suffix}";
        }

        /// <summary>Valida que el usuario pertenece al departamento actual de la instancia antes de escribir.</summary>
        private static void RequireInstanceWrite(DocflowWorkflowInstance inst, int deptId)
        {
            if (inst.CurrentDepartmentId != deptId)
                throw new UnauthorizedAccessException(
                    $"El departamento {deptId} no tiene permisos de escritura sobre la instancia {inst.InstanceId}.");
        }

        /// <summary>Obtiene la instancia validando que el departamento tiene acceso de lectura.</summary>
        private async Task<DocflowWorkflowInstance> RequireInstanceReadAsync(
            Guid instanceId,
            int deptId,
            CancellationToken ct)
        {
            var inst = await _instRepo.GetByIdAsync(instanceId, ct)
                ?? throw new KeyNotFoundException($"Instancia {instanceId} no encontrada.");

            // TODO: Ampliar con lógica de participación multi-departamento si aplica
            // Ejemplo: verificar tabla de participantes o historial de movimientos
            return inst;
        }

        /// <summary>Construye la ruta relativa de almacenamiento para un archivo.</summary>
        private static string BuildRelativePath(string folder, Guid instId, Guid docId)
        {
            var baseFolder = string.IsNullOrWhiteSpace(folder) ? EntityType.ToLowerInvariant() : folder;
            return $"{baseFolder}/{instId:N}/{EntityType.ToLowerInvariant()}/{docId:N}";
        }

        /// <summary>
        /// Valida que el JSON de metadata dinámica es compatible con el esquema del proceso.
        /// </summary>
        private async Task ValidateDynamicMetadataAgainstProcessSchemaAsync(
            int processId,
            string? json,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            var schemaJson = await _procRepo.GetDynamicFieldMetadataJsonAsync(processId, ct);
            if (string.IsNullOrWhiteSpace(schemaJson) || schemaJson == "[]")
                return;

            List<DynamicFieldSchemaDto>? schema;
            try
            {
                schema = JsonSerializer.Deserialize<List<DynamicFieldSchemaDto>>(schemaJson, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex,
                    "Esquema JSON inválido para proceso {ProcessId}. Se omite validación de metadata.", processId);
                return;
            }

            if (schema is null || schema.Count == 0)
                return;

            JsonDocument? doc;
            try
            {
                doc = JsonDocument.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("El JSON de metadata dinámica no es válido.", nameof(json), ex);
            }

            foreach (var field in schema.Where(f => f.Required))
            {
                if (!doc.RootElement.TryGetProperty(field.Name, out var value) ||
                    value.ValueKind == JsonValueKind.Null ||
                    value.ValueKind == JsonValueKind.Undefined)
                {
                    throw new ArgumentException(
                        $"El campo requerido '{field.Name}' no está presente en la metadata dinámica.");
                }
            }
        }

        #endregion
    }
}
using AutoMapper;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Documents;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.DTOs.StoredFile;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    /// <summary>
    /// Orquesta la operación “completa”:
    /// 1) Subir físico (NAS/FTP) via IFileManagementService
    /// 2) Registrar metadata en DB via IStoredFileService
    /// 3) Responder en formato listo para UI (React)
    ///
    /// Política: Best-effort.
    /// - Si un archivo sube físico pero falla DB, se intenta borrar físico.
    /// </summary>
    public sealed class DocumentOrchestratorService : IDocumentOrchestratorService
    {
        private readonly IFileManagementService _fileManagement;
        private readonly IStoredFileService _storedFileService;
        private readonly ILogger<DocumentOrchestratorService> _logger;
        private readonly IMapper _mapper;
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserActionPermissionService _permissionService;

        // Estados en los que todavía se puede adjuntar archivos desde las pantallas de Nueva
        // Acción/Nuevo Contrato (fuera de estos, el registro ya avanzó de trámite — solo debe
        // poder visualizar lo ya cargado; ajustar documentos en ese punto es responsabilidad de
        // la página de Corrección, ver CheckUploadBlockedAsync).
        private static readonly HashSet<string> EditableActionStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "BORRADOR", "GENERADO", "DRAFT" };
        private static readonly HashSet<string> EditableContractStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "BORRADOR", "GENERADO" };
        private const string ContractStatusCategory = "CONTRACT_STATUS";

        public DocumentOrchestratorService(
            IFileManagementService fileManagement,
            IStoredFileService storedFileService,
            ILogger<DocumentOrchestratorService> logger,
            IMapper mapper,
            AppDbContext db,
            IHttpContextAccessor httpContextAccessor,
            IUserActionPermissionService permissionService)
        {
            _fileManagement = fileManagement;
            _storedFileService = storedFileService;
            _logger = logger;
            _mapper = mapper;
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _permissionService = permissionService;
        }

        /// <summary>
        /// Null si la subida puede continuar; si no, el mensaje a devolver al cliente explicando
        /// por qué se bloqueó. Solo se aplica a EntityType conocidos con concepto de "trámite en
        /// curso" (PersonnelAction/HRCONTRACT); cualquier otro EntityType (hoja de vida, firmas,
        /// certificación financiera, etc.) no tiene esta restricción y sigue igual que antes.
        /// </summary>
        private async Task<string?> CheckUploadBlockedAsync(string entityType, string entityId, CancellationToken ct)
        {
            switch (entityType)
            {
                case "PersonnelAction":
                {
                    if (!int.TryParse(entityId, out var actionId)) return null;

                    var status = await _db.PersonnelActions.AsNoTracking()
                        .Where(a => a.ActionId == actionId)
                        .Select(a => a.Status)
                        .FirstOrDefaultAsync(ct);

                    if (status is null || EditableActionStatuses.Contains(status)) return null;
                    if (await HasCorrectionPermissionAsync("PERSONNEL_ACTIONS.MANAGE", ct)) return null;

                    return $"No se pueden adjuntar archivos: la acción está en estado '{status}'. " +
                           "Use la página de Corrección para modificar documentos de acciones ya avanzadas.";
                }

                case "HRCONTRACT":
                {
                    if (!int.TryParse(entityId, out var contractId)) return null;

                    var statusTypeId = await _db.Set<Contracts>().AsNoTracking()
                        .Where(c => c.ContractID == contractId)
                        .Select(c => (int?)c.Status)
                        .FirstOrDefaultAsync(ct);

                    if (statusTypeId is null) return null;

                    var statusName = await _db.RefTypes.AsNoTracking()
                        .Where(r => r.TypeId == statusTypeId.Value && r.Category == ContractStatusCategory)
                        .Select(r => r.Name)
                        .FirstOrDefaultAsync(ct);

                    if (statusName is null || EditableContractStatuses.Contains(statusName)) return null;
                    if (await HasCorrectionPermissionAsync("CONTRACTS.MANAGE", ct)) return null;

                    return $"No se pueden adjuntar archivos: el contrato está en estado '{statusName}'. " +
                           "Use la página de Corrección para modificar documentos de contratos ya avanzados.";
                }

                default:
                    return null;
            }
        }

        /// <summary>
        /// Verificado en servidor contra los roles reales del token (mismo mecanismo que
        /// RequirePermissionAttribute) — nunca un flag que mande el cliente. Los usuarios con
        /// este permiso elevado son quienes usan la página de Corrección, así que necesitan
        /// poder adjuntar/reemplazar documentos sin importar el estado del registro.
        /// </summary>
        private async Task<bool> HasCorrectionPermissionAsync(string permissionCode, CancellationToken ct)
        {
            var roles = _httpContextAccessor.HttpContext?.User
                .FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();

            return await _permissionService.HasPermissionAsync(roles, permissionCode, ct);
        }

        // (1) MISMO TIPO PARA VARIOS ARCHIVOS (BATCH)
        public async Task<DocumentUploadResultDto> UploadAndRegisterAsync(DocumentUploadRequestDto request, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            var validation = ValidateCommon(request.DirectoryCode, request.EntityType, request.EntityId);
            if (validation != null)
            {
                return new DocumentUploadResultDto
                {
                    Success = false,
                    Message = validation,
                    Total = request.Files?.Count ?? 0
                };
            }

            var blockedReason = await CheckUploadBlockedAsync(request.EntityType, request.EntityId, ct);
            if (blockedReason != null)
            {
                return new DocumentUploadResultDto
                {
                    Success = false,
                    Message = blockedReason,
                    Total = request.Files?.Count ?? 0
                };
            }

            if (request.Files == null || request.Files.Count == 0)
            {
                return new DocumentUploadResultDto
                {
                    Success = false,
                    Message = "No se enviaron archivos.",
                    Total = 0
                };
            }

            var uploadMultiple = new FileUploadMultipleRequestDto
            {
                DirectoryCode = request.DirectoryCode,
                RelativePath = NormalizeRelativePath(request.RelativePath),
                Files = request.Files
            };

            var physicalResults = await _fileManagement.UploadMultipleFilesAsync(uploadMultiple, ct);

            var result = new DocumentUploadResultDto { Total = physicalResults.Count };

            // ✅ Map por índice: request.Files[i] ↔ physicalResults[i]
            for (int i = 0; i < physicalResults.Count; i++)
            {
                var pr = physicalResults[i];
                var original = i < request.Files.Count ? request.Files[i] : null;

                var item = new DocumentUploadItemResultDto
                {
                    Success = pr.Success,
                    Message = pr.Message,
                    OriginalFileName = original?.FileName ?? pr.FileName ?? string.Empty,
                    SizeBytes = pr.FileSize,
                    PhysicalRelativePath = pr.RelativePath
                };

                if (!pr.Success)
                {
                    result.Items.Add(item);
                    continue;
                }

                try
                {
                    var (relativeFolder, storedFileName) = SplitFolderAndFile(pr.RelativePath, pr.FileName);

                    var entity = new StoredFile
                    {
                        DirectoryCode = request.DirectoryCode,
                        EntityType = request.EntityType,
                        EntityId = request.EntityId,

                        UploadYear = pr.Year,
                        RelativeFolder = relativeFolder,
                        StoredFileName = storedFileName,

                        OriginalFileName = original?.FileName ?? storedFileName,
                        Extension = Path.GetExtension(storedFileName),
                        ContentType = original?.ContentType,
                        SizeBytes = pr.FileSize,
                        Status = 1,
                        CreatedAt = DateTime.Now,

                        // ✅ tipo aplicado al batch
                        DocumentTypeId = request.DocumentTypeId,
                        DocumentReferenceNumber = request.DocumentReferenceNumber,
                        DocumentReferenceDate = request.DocumentReferenceDate
                    };

                    var created = await _storedFileService.CreateAsync(entity, ct);

                    item.StoredFile = _mapper.Map<StoredFileDto>(created);
                    item.Success = true;
                    item.Message = "OK";
                    result.Uploaded++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "DB register failed (batch). DirectoryCode={DirectoryCode}, RelativePath={RelativePath}, FileName={FileName}",
                        request.DirectoryCode, pr.RelativePath, pr.FileName);

                    item.Success = false;
                    item.Message = "Error registrando en DB. Se intentó revertir el archivo físico.";
                    await TryPhysicalCleanupAsync(request.DirectoryCode, pr.RelativePath, ct);
                    result.Failed++;
                }

                result.Items.Add(item);
            }

            FinalizeResult(result, sw);
            return result;
        }

        // (3) SINGLE
        public async Task<DocumentUploadResultDto> UploadSingleAndRegisterAsync(DocumentUploadSingleRequestDto request, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            var validation = ValidateCommon(request.DirectoryCode, request.EntityType, request.EntityId);
            if (validation != null)
            {
                return new DocumentUploadResultDto { Success = false, Message = validation, Total = 1 };
            }

            var blockedReason = await CheckUploadBlockedAsync(request.EntityType, request.EntityId, ct);
            if (blockedReason != null)
            {
                return new DocumentUploadResultDto { Success = false, Message = blockedReason, Total = 1 };
            }

            if (request.File == null || request.File.Length == 0)
            {
                return new DocumentUploadResultDto { Success = false, Message = "No se envió archivo.", Total = 0 };
            }

            var uploadReq = new FileUploadRequestDto
            {
                DirectoryCode = request.DirectoryCode,
                RelativePath = NormalizeRelativePath(request.RelativePath),
                FileName = request.File.FileName,
                File = request.File
            };

            var pr = await _fileManagement.UploadFileAsync(uploadReq, ct);

            var result = new DocumentUploadResultDto { Total = 1 };

            var item = new DocumentUploadItemResultDto
            {
                Success = pr.Success,
                Message = pr.Message,
                OriginalFileName = request.File.FileName,
                SizeBytes = request.File.Length,
                PhysicalRelativePath = pr.RelativePath
            };

            if (!pr.Success)
            {
                result.Items.Add(item);
                FinalizeResult(result, sw);
                return result;
            }

            try
            {
                var (relativeFolder, storedFileName) = SplitFolderAndFile(pr.RelativePath, pr.FileName);

                var entity = new StoredFile
                {
                    DirectoryCode = request.DirectoryCode,
                    EntityType = request.EntityType,
                    EntityId = request.EntityId,

                    UploadYear = pr.Year,
                    RelativeFolder = relativeFolder,
                    StoredFileName = storedFileName,

                    OriginalFileName = request.File.FileName,
                    Extension = Path.GetExtension(storedFileName),
                    ContentType = request.File.ContentType,
                    SizeBytes = pr.FileSize,
                    Status = 1,
                    CreatedAt = DateTime.Now,

                    DocumentTypeId = request.DocumentTypeId,
                    DocumentReferenceNumber = request.DocumentReferenceNumber,
                    DocumentReferenceDate = request.DocumentReferenceDate
                };

                var created = await _storedFileService.CreateAsync(entity, ct);
                item.StoredFile = _mapper.Map<StoredFileDto>(created);
                item.Success = true;
                item.Message = "OK";
                result.Uploaded = 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "DB register failed (single). DirectoryCode={DirectoryCode}, RelativePath={RelativePath}, FileName={FileName}",
                    request.DirectoryCode, pr.RelativePath, pr.FileName);

                item.Success = false;
                item.Message = "Error registrando en DB. Se intentó revertir el archivo físico.";
                await TryPhysicalCleanupAsync(request.DirectoryCode, pr.RelativePath, ct);
                result.Failed = 1;
            }

            result.Items.Add(item);
            FinalizeResult(result, sw);
            return result;
        }

        // (2) MAPPED BATCH (tipo por archivo)
        public async Task<DocumentUploadResultDto> UploadMappedAndRegisterAsync(DocumentUploadMappedRequestDto request, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();

            var validation = ValidateCommon(request.DirectoryCode, request.EntityType, request.EntityId);
            if (validation != null)
            {
                return new DocumentUploadResultDto
                {
                    Success = false,
                    Message = validation,
                    Total = request.Items?.Count ?? 0
                };
            }

            var blockedReason = await CheckUploadBlockedAsync(request.EntityType, request.EntityId, ct);
            if (blockedReason != null)
            {
                return new DocumentUploadResultDto
                {
                    Success = false,
                    Message = blockedReason,
                    Total = request.Items?.Count ?? 0
                };
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return new DocumentUploadResultDto { Success = false, Message = "No se enviaron archivos.", Total = 0 };
            }

            var result = new DocumentUploadResultDto { Total = request.Items.Count };
            var baseRelative = NormalizeRelativePath(request.RelativePath);

            for (int i = 0; i < request.Items.Count; i++)
            {
                var it = request.Items[i];

                if (it.File == null || it.File.Length == 0)
                {
                    result.Items.Add(new DocumentUploadItemResultDto
                    {
                        Success = false,
                        Message = "Archivo vacío o nulo.",
                        OriginalFileName = it.File?.FileName ?? string.Empty
                    });
                    continue;
                }

                var uploadReq = new FileUploadRequestDto
                {
                    DirectoryCode = request.DirectoryCode,
                    RelativePath = baseRelative,
                    FileName = it.File.FileName,
                    File = it.File
                };

                var pr = await _fileManagement.UploadFileAsync(uploadReq, ct);

                var item = new DocumentUploadItemResultDto
                {
                    Success = pr.Success,
                    Message = pr.Message,
                    OriginalFileName = it.File.FileName,
                    SizeBytes = it.File.Length,
                    PhysicalRelativePath = pr.RelativePath
                };

                if (!pr.Success)
                {
                    result.Items.Add(item);
                    continue;
                }

                try
                {
                    var (relativeFolder, storedFileName) = SplitFolderAndFile(pr.RelativePath, pr.FileName);

                    var entity = new StoredFile
                    {
                        DirectoryCode = request.DirectoryCode,
                        EntityType = request.EntityType,
                        EntityId = request.EntityId,

                        UploadYear = pr.Year,
                        RelativeFolder = relativeFolder,
                        StoredFileName = storedFileName,

                        OriginalFileName = it.File.FileName,
                        Extension = Path.GetExtension(storedFileName),
                        ContentType = it.File.ContentType,
                        SizeBytes = pr.FileSize,
                        Status = 1,
                        CreatedAt = DateTime.Now,

                        DocumentTypeId = it.DocumentTypeId,
                        DocumentReferenceNumber = it.DocumentReferenceNumber,
                        DocumentReferenceDate = it.DocumentReferenceDate
                    };

                    var created = await _storedFileService.CreateAsync(entity, ct);
                    item.StoredFile = _mapper.Map<StoredFileDto>(created);
                    item.Success = true;
                    item.Message = "OK";
                    result.Uploaded++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "DB register failed (mapped). DirectoryCode={DirectoryCode}, RelativePath={RelativePath}, FileName={FileName}, Index={Index}",
                        request.DirectoryCode, pr.RelativePath, pr.FileName, i);

                    item.Success = false;
                    item.Message = "Error registrando en DB. Se intentó revertir el archivo físico.";
                    await TryPhysicalCleanupAsync(request.DirectoryCode, pr.RelativePath, ct);
                    result.Failed++;
                }

                result.Items.Add(item);
            }

            FinalizeResult(result, sw);
            return result;
        }

        // -----------------------
        // LIST/DOWNLOAD/DELETE (sin cambios)
        // -----------------------
        public Task<List<StoredFile>> ListByEntityAsync(string directoryCode, string entityType, string entityId, int? uploadYear, int? status, CancellationToken ct)
            => _storedFileService.GetByEntityAsync(directoryCode, entityType, entityId, uploadYear, status, ct)
                .ContinueWith(t => t.Result.ToList(), ct);

        public async Task<(byte[] fileBytes, string contentType, string fileName)?> DownloadByGuidAsync(Guid fileGuid, CancellationToken ct)
        {
            var db = await _storedFileService.GetByGuidAsync(fileGuid, ct);
            if (db is null) return null;

            var filePath = BuildFilePathForStorage(db.RelativeFolder, db.StoredFileName);
            return await _fileManagement.DownloadFileAsync(db.DirectoryCode, filePath, ct);
        }

        public async Task<bool> DeleteByGuidAsync(Guid fileGuid, bool deletePhysical, int? deletedBy, CancellationToken ct)
        {
            var db = await _storedFileService.GetByGuidAsync(fileGuid, ct);
            if (db is null) return false;

            await _storedFileService.SoftDeleteAsync(db.FileId, deletedBy, ct);

            if (deletePhysical)
            {
                var filePath = BuildFilePathForStorage(db.RelativeFolder, db.StoredFileName);
                var del = await _fileManagement.DeleteFileAsync(db.DirectoryCode, filePath, ct);
                if (!del.Success)
                {
                    _logger.LogWarning("Physical delete failed. FileGuid={FileGuid}, DirectoryCode={DirectoryCode}, Path={Path}, Msg={Msg}",
                        fileGuid, db.DirectoryCode, filePath, del.Message);
                }
            }

            return true;
        }

        // -----------------------
        // Helpers
        // -----------------------
        private static string? ValidateCommon(string directoryCode, string entityType, string entityId)
        {
            if (string.IsNullOrWhiteSpace(directoryCode) ||
                string.IsNullOrWhiteSpace(entityType) ||
                string.IsNullOrWhiteSpace(entityId))
                return "DirectoryCode, EntityType y EntityId son requeridos.";
            return null;
        }

        private static void FinalizeResult(DocumentUploadResultDto result, Stopwatch sw)
        {
            sw.Stop();
            result.Failed = result.Items.Count(x => !x.Success);
            result.Uploaded = result.Items.Count(x => x.Success);
            result.Success = result.Failed == 0;

            result.Message = result.Success
                ? $"Carga completa OK. Archivos={result.Uploaded}. ElapsedMs={sw.ElapsedMilliseconds}"
                : $"Carga parcial. OK={result.Uploaded}, Fail={result.Failed}. ElapsedMs={sw.ElapsedMilliseconds}";
        }

        private async Task TryPhysicalCleanupAsync(string directoryCode, string? relativePath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            try
            {
                await _fileManagement.DeleteFileAsync(directoryCode, relativePath, ct);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx,
                    "Physical cleanup failed. DirectoryCode={DirectoryCode}, RelativePath={RelativePath}",
                    directoryCode, relativePath);
            }
        }

        private static string NormalizeRelativePath(string? relativePath)
        {
            // Sin agrupación automática por EntityType/EntityId: si el caller no especifica
            // una subcarpeta, el archivo cae directo en {PhysicalPath}/{año}/archivo, según
            // el directorio parametrizado en HR.TBL_DirectoryParameters.
            return string.IsNullOrWhiteSpace(relativePath)
                ? string.Empty
                : relativePath.Trim().TrimStart('/').TrimEnd('/');
        }

        private static (string relativeFolder, string storedFileName) SplitFolderAndFile(string? relativePath, string? fileName)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                var f = string.IsNullOrWhiteSpace(fileName) ? "file" : Path.GetFileName(fileName);
                return (string.Empty, f);
            }

            var sanitized = relativePath.Trim().TrimStart('/');
            var lastSlash = sanitized.LastIndexOf('/');
            if (lastSlash < 0)
            {
                var onlyFile = Path.GetFileName(sanitized);
                return (string.Empty, onlyFile);
            }

            var folder = sanitized.Substring(0, lastSlash + 1);
            var file = sanitized.Substring(lastSlash + 1);

            if (!folder.EndsWith("/")) folder += "/";
            return (folder, file);
        }

        private static string BuildFilePathForStorage(string relativeFolder, string storedFileName)
        {
            var folder = (relativeFolder ?? string.Empty).Trim().TrimStart('/');
            if (!string.IsNullOrEmpty(folder) && !folder.EndsWith("/"))
                folder += "/";

            var file = Path.GetFileName(storedFileName ?? string.Empty);
            return "/" + folder + file;
        }
    }
}
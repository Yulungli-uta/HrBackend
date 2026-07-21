using Microsoft.Extensions.Options;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Configuration;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Application.Services;

public class FileManagementService : IFileManagementService
{
    private readonly IDirectoryParametersService _directoryService;
    private readonly IEncryptionService _encryptionService;
    private readonly FileManagementSettings _settings;
    private readonly ILogger<FileManagementService> _logger;

    public FileManagementService(
        IDirectoryParametersService directoryService,
        IEncryptionService encryptionService,
        IOptions<FileManagementSettings> settings,
        ILogger<FileManagementService> logger)
    {
        _directoryService = directoryService;
        _encryptionService = encryptionService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<FileUploadResponseDto> UploadFileAsync(FileUploadRequestDto request, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Upload] Inicio. DirectoryCode={DirectoryCode} FileName={FileName} Size={Size}B",
            request.DirectoryCode, request.FileName, request.File?.Length ?? 0);

        try
        {
            // 1. Buscar DirectoryParameters por Code
            _logger.LogDebug("[Upload] Paso 1 — buscando DirectoryParameters para '{Code}'", request.DirectoryCode);
            var directory = await _directoryService.GetByCodeAsync(request.DirectoryCode, ct);
            if (directory == null)
            {
                _logger.LogWarning("[Upload] DirectoryParameters no encontrado para '{Code}'", request.DirectoryCode);
                return CreateErrorResponse("Directory not found", request.FileName);
            }

            _logger.LogDebug(
                "[Upload] Paso 1 OK — PhysicalPath='{Path}' MaxSizeMb={Max} Extension='{Ext}'",
                directory.PhysicalPath, directory.MaxSizeMb, directory.Extension);

            // 2. Validar extensión del archivo
            var originalFileName = Path.GetFileName(request.FileName);
            var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
            _logger.LogDebug("[Upload] Paso 2 — extensión detectada: '{Ext}'", fileExtension);

            if (!string.IsNullOrEmpty(directory.Extension))
            {
                var allowedExtensions = directory.Extension.Split(',')
                    .Select(e => e.Trim().ToLowerInvariant())
                    .ToList();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning(
                        "[Upload] Extensión rechazada. Recibida='{Ext}' Permitidas='{Allowed}'",
                        fileExtension, directory.Extension);
                    return CreateErrorResponse(
                        $"File extension '{fileExtension}' is not allowed. Allowed: {directory.Extension}",
                        originalFileName);
                }
            }

            // 3. Validar tamaño del archivo
            var fileSizeInMb = request.File.Length / (1024.0 * 1024.0);
            _logger.LogDebug("[Upload] Paso 3 — tamaño {Size:F2} MB", fileSizeInMb);

            if (directory.MaxSizeMb.HasValue && fileSizeInMb > directory.MaxSizeMb.Value)
            {
                _logger.LogWarning(
                    "[Upload] Tamaño excedido. Recibido={Size:F2}MB Máximo={Max}MB",
                    fileSizeInMb, directory.MaxSizeMb);
                return CreateErrorResponse(
                    $"File size ({fileSizeInMb:F2} MB) exceeds maximum ({directory.MaxSizeMb} MB)",
                    originalFileName);
            }

            // 4. Preparar rutas (ResolveSafePath evita path traversal vía RelativePath)
            int currentYear = DateTime.Now.Year;
            var relativePath = request.RelativePath.TrimStart('/').TrimEnd('/');
            var safeRelativeBase = ResolveSafePath(directory.PhysicalPath, relativePath);
            var folderPath = Path.Combine(safeRelativeBase, currentYear.ToString());

            var storedFileName = FileNameGenerator.Generate(originalFileName, request.DirectoryCode);
            var fullPath = Path.Combine(folderPath, storedFileName);

            _logger.LogDebug(
                "[Upload] Paso 4 — rutas preparadas. FolderPath='{Folder}' FullPath='{Full}'",
                folderPath, fullPath);

            // 4b. Verificar accesibilidad de la ruta base (timeout 8 s para no bloquear en red inaccesible)
            _logger.LogDebug("[Upload] Paso 4b — verificando accesibilidad de '{BasePath}'", directory.PhysicalPath);
            bool pathOk = await IsPathAccessibleAsync(directory.PhysicalPath, ct);
            if (!pathOk)
            {
                var shareRoot = (Path.GetPathRoot(directory.PhysicalPath) ?? directory.PhysicalPath).TrimEnd('\\', '/');
                _logger.LogError(
                    "[Upload] Share raíz no accesible: '{Share}'. " +
                    "Verifica montaje de red (net use), credenciales (UseImpersonation) o permisos de la cuenta de servicio.",
                    shareRoot);
                return CreateErrorResponse(
                    $"El share de red '{shareRoot}' no está accesible desde el servidor. " +
                    "Verifica la conexión al NAS o habilita UseImpersonation con credenciales válidas.",
                    originalFileName);
            }
            _logger.LogDebug("[Upload] Paso 4b OK — ruta base accesible.");

            // 5. Ejecutar operación con o sin impersonation según configuración
            _logger.LogDebug(
                "[Upload] Paso 5 — guardando archivo. UseImpersonation={Imp}",
                _settings.UseImpersonation);

            if (_settings.UseImpersonation)
            {
                var (username, password, domain) = DecryptCredentials();
                using var impersonation = new WindowsImpersonation();

                await impersonation.RunImpersonatedAsync(username, password, domain, async () =>
                {
                    await SaveFileAsync(folderPath, fullPath, request.File, ct);
                });
            }
            else
            {
                await SaveFileAsync(folderPath, fullPath, request.File, ct);
            }

            _logger.LogInformation(
                "[Upload] Archivo guardado exitosamente. FullPath='{Full}' Size={Size}B",
                fullPath, request.File.Length);

            // 6. Retornar respuesta exitosa
            var relativePathResult = $"/{relativePath}/{currentYear}/{storedFileName}";

            return new FileUploadResponseDto
            {
                Success = true,
                Message = "File uploaded successfully.",
                FullPath = fullPath,
                RelativePath = relativePathResult,
                FileName = storedFileName,
                FileSize = request.File.Length,
                Year = currentYear
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "[Upload] Cancelado por el cliente. DirectoryCode={Code} FileName={File}",
                request.DirectoryCode, request.FileName);
            return CreateErrorResponse("La operación fue cancelada (timeout del cliente).", request.FileName);
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogError(ex, "[Upload] PlatformNotSupported. DirectoryCode={Code}", request.DirectoryCode);
            return CreateErrorResponse($"Platform not supported: {ex.Message}", request.FileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[Upload] Autenticación fallida. DirectoryCode={Code}", request.DirectoryCode);
            return CreateErrorResponse($"Authentication failed: {ex.Message}", request.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[Upload] Error inesperado. DirectoryCode={Code} FileName={File}",
                request.DirectoryCode, request.FileName);
            return CreateErrorResponse($"Error uploading file: {ex.Message}", request.FileName);
        }
    }

    public async Task<List<FileUploadResponseDto>> UploadMultipleFilesAsync(FileUploadMultipleRequestDto request, CancellationToken ct = default)
    {
        var results = new List<FileUploadResponseDto>();

        if (request.Files == null || !request.Files.Any())
        {
            results.Add(CreateErrorResponse("No files provided", string.Empty));
            return results;
        }

        // Procesar cada archivo
        foreach (var file in request.Files)
        {
            var uploadRequest = new FileUploadRequestDto
            {
                DirectoryCode = request.DirectoryCode,
                RelativePath = request.RelativePath,
                FileName = file.FileName,
                File = file
            };

            var result = await UploadFileAsync(uploadRequest, ct);
            results.Add(result);
        }

        return results;
    }

    public async Task<(byte[] fileBytes, string contentType, string fileName)?> DownloadFileAsync(
        string directoryCode,
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Buscar DirectoryParameters por Code
            var directory = await _directoryService.GetByCodeAsync(directoryCode, ct);
            if (directory == null) return null;

            // 2. Sanitizar y construir ruta (evita path traversal vía filePath)
            var fullPath = ResolveSafePath(directory.PhysicalPath, filePath);

            byte[] fileBytes;

            // 3. Ejecutar operación con o sin impersonation según configuración
            if (_settings.UseImpersonation)
            {
                // CON CREDENCIALES (NAS remoto con autenticación)
                var (username, password, domain) = DecryptCredentials();
                using var impersonation = new WindowsImpersonation();

                fileBytes = await impersonation.RunImpersonatedAsync(username, password, domain, async () =>
                {
                    return await ReadFileAsync(fullPath, ct);
                });
            }
            else
            {
                // SIN CREDENCIALES (punto de montaje local o acceso directo)
                fileBytes = await ReadFileAsync(fullPath, ct);
            }

            if (fileBytes.Length == 0) return null;


            // 4. Determinar Content-Type
            var contentType = GetContentType(fullPath);
            var fileName = Path.GetFileName(fullPath);

            return (fileBytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Download failed. DirectoryCode={DirectoryCode}, FilePath={FilePath}",
                directoryCode, filePath);
            return null;
        }
    }

    public async Task<FileDeleteResponseDto> DeleteFileAsync(
        string directoryCode,
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            // 1. Buscar DirectoryParameters por Code
            var directory = await _directoryService.GetByCodeAsync(directoryCode, ct);
            if (directory == null)
            {
                return new FileDeleteResponseDto
                {
                    Success = false,
                    Message = $"Directory with code '{directoryCode}' not found or inactive.",
                    FilePath = filePath
                };
            }

            // 2. Sanitizar y construir ruta (evita path traversal vía filePath)
            var fullPath = ResolveSafePath(directory.PhysicalPath, filePath);

            bool deleted;

            // 3. Ejecutar operación con o sin impersonation según configuración
            if (_settings.UseImpersonation)
            {
                // CON CREDENCIALES (NAS remoto con autenticación)
                var (username, password, domain) = DecryptCredentials();
                using var impersonation = new WindowsImpersonation();

                deleted = await impersonation.RunImpersonatedAsync(username, password, domain, async () =>
                {
                    return await DeleteFileInternalAsync(fullPath, ct);
                });
            }
            else
            {
                // SIN CREDENCIALES (punto de montaje local o acceso directo)
                deleted = await DeleteFileInternalAsync(fullPath, ct);
            }

            if (!deleted)
            {
                return new FileDeleteResponseDto
                {
                    Success = false,
                    Message = "File not found.",
                    FilePath = filePath
                };
            }

            return new FileDeleteResponseDto
            {
                Success = true,
                Message = "File deleted successfully.",
                FilePath = filePath
            };
        }
        catch (PlatformNotSupportedException ex)
        {
            return new FileDeleteResponseDto
            {
                Success = false,
                Message = $"Platform not supported: {ex.Message}",
                FilePath = filePath
            };
        }
        catch (InvalidOperationException ex)
        {
            return new FileDeleteResponseDto
            {
                Success = false,
                Message = $"Authentication failed: {ex.Message}",
                FilePath = filePath
            };
        }
        catch (Exception ex)
        {
            return new FileDeleteResponseDto
            {
                Success = false,
                Message = $"Error deleting file: {ex.Message}",
                FilePath = filePath
            };
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Combina basePath con relativePath y garantiza que la ruta resultante no escape
    /// del directorio base (protección contra path traversal vía "..", rutas absolutas, etc.).
    /// </summary>
    private static string ResolveSafePath(string basePath, string relativePath)
    {
        var sanitized = (relativePath ?? string.Empty).TrimStart('/', '\\');
        var normalizedBase = Path.GetFullPath(basePath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var resolved = Path.GetFullPath(Path.Combine(normalizedBase, sanitized));

        if (!resolved.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Ruta fuera del directorio permitido.");
        }

        return resolved;
    }

    /// <summary>
    /// Guarda un archivo en el sistema de archivos.
    /// Crea la estructura de carpetas automáticamente si no existe.
    /// </summary>
    private static async Task SaveFileAsync(string folderPath, string fullPath, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct)
    {
        // Crear árbol de carpetas si no existe (operación síncrona en thread pool)
        await Task.Run(() => Directory.CreateDirectory(folderPath), ct);

        // Guardar archivo
        using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
        await file.CopyToAsync(stream, ct);
    }

    /// <summary>
    /// Lee un archivo del sistema de archivos
    /// </summary>
    private static async Task<byte[]> ReadFileAsync(string fullPath, CancellationToken ct)
    {
        // Verificar existencia
        if (!File.Exists(fullPath))
        {
            return Array.Empty<byte>();
        }

        // Leer archivo
        return await File.ReadAllBytesAsync(fullPath, ct);
    }

    /// <summary>
    /// Elimina un archivo del sistema de archivos
    /// </summary>
    private static async Task<bool> DeleteFileInternalAsync(string fullPath, CancellationToken ct)
    {
        // Verificar existencia
        if (!File.Exists(fullPath))
        {
            return false;
        }

        // Eliminar archivo (usar Task.Run para operación síncrona)
        await Task.Run(() => File.Delete(fullPath), ct);
        return true;
    }

    /// <summary>
    /// Verifica que el share raíz es accesible con timeout de 8 s.
    /// Para rutas UNC (\\server\share\sub\dir) solo comprueba \\server\share —
    /// los subdirectorios no necesitan existir porque Directory.CreateDirectory los crea.
    /// </summary>
    private async Task<bool> IsPathAccessibleAsync(string basePath, CancellationToken ct)
    {
        // Para rutas UNC extraer solo \\server\share; para rutas locales usar la raíz (C:\)
        var root = Path.GetPathRoot(basePath) ?? basePath;
        var checkPath = root.StartsWith(@"\\", StringComparison.Ordinal)
            ? root.TrimEnd('\\', '/')
            : root;

        _logger.LogDebug(
            "[Upload] Verificando accesibilidad en share raíz '{Check}' (ruta completa: '{Full}')",
            checkPath, basePath);

        try
        {
            return await Task.Run(() =>
            {
                try { return Directory.Exists(checkPath); }
                catch { return false; }
            }).WaitAsync(TimeSpan.FromSeconds(8), ct);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("[Upload] Timeout verificando share raíz '{Path}' (>8 s)", checkPath);
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Upload] Excepción al verificar share raíz '{Path}'", checkPath);
            return false;
        }
    }

    /// <summary>
    /// Desencripta las credenciales de red desde la configuración
    /// </summary>
    private (string username, string password, string? domain) DecryptCredentials()
    {
        var username = _encryptionService.Decrypt(_settings.NetworkCredentials.Username);
        var password = _encryptionService.Decrypt(_settings.NetworkCredentials.Password);
        var domain = string.IsNullOrEmpty(_settings.NetworkCredentials.Domain)
            ? null
            : _encryptionService.Decrypt(_settings.NetworkCredentials.Domain);

        return (username, password, domain);
    }

    /// <summary>
    /// Crea una respuesta de error para upload
    /// </summary>
    private static FileUploadResponseDto CreateErrorResponse(string message, string fileName)
    {
        return new FileUploadResponseDto
        {
            Success = false,
            Message = message,
            FullPath = string.Empty,
            RelativePath = string.Empty,
            FileName = fileName,
            FileSize = 0,
            Year = 0
        };
    }

    /// <summary>
    /// Determina el Content-Type basado en la extensión del archivo
    /// </summary>
    private static string GetContentType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xls" => "application/vnd.ms-excel",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            ".7z" => "application/x-7z-compressed",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            _ => "application/octet-stream"
        };
    }

    #endregion
}


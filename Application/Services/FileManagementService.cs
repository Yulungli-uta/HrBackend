using Microsoft.Extensions.Options;
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

    public FileManagementService(
        IDirectoryParametersService directoryService,
        IEncryptionService encryptionService,
        IOptions<FileManagementSettings> settings)
    {
        _directoryService = directoryService;
        _encryptionService = encryptionService;
        _settings = settings.Value;
    }

    public async Task<FileUploadResponseDto> UploadFileAsync(FileUploadRequestDto request, CancellationToken ct = default)
    {
        try
        {
            // 1. Buscar DirectoryParameters por Code
            var directory = await _directoryService.GetByCodeAsync(request.DirectoryCode, ct);
            if (directory == null)
            {
                return CreateErrorResponse("Directory not found", request.FileName);
            }

            // 2. Validar extensión del archivo
            var fileExtension = Path.GetExtension(request.FileName).ToLower();
            if (!string.IsNullOrEmpty(directory.Extension))
            {
                var allowedExtensions = directory.Extension.Split(',').Select(e => e.Trim().ToLower()).ToList();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return CreateErrorResponse(
                        $"File extension '{fileExtension}' is not allowed. Allowed: {directory.Extension}", 
                        request.FileName);
                }
            }

            // 3. Validar tamaño del archivo
            var fileSizeInMb = request.File.Length / (1024.0 * 1024.0);
            if (directory.MaxSizeMb.HasValue && fileSizeInMb > directory.MaxSizeMb.Value)
            {
                return CreateErrorResponse(
                    $"File size ({fileSizeInMb:F2} MB) exceeds maximum ({directory.MaxSizeMb} MB)", 
                    request.FileName);
            }

            // 4. Preparar rutas
            int currentYear = DateTime.Now.Year;
            var relativePath = request.RelativePath.TrimStart('/').TrimEnd('/');
            var folderPath = Path.Combine(directory.PhysicalPath, relativePath, currentYear.ToString());
            var sanitizedFileName = Path.GetFileName(request.FileName);
            var fullPath = Path.Combine(folderPath, sanitizedFileName);

            // 5. Ejecutar operación con o sin impersonation según configuración
            if (_settings.UseImpersonation)
            {
                // CON CREDENCIALES (NAS remoto con autenticación)
                var (username, password, domain) = DecryptCredentials();
                using var impersonation = new WindowsImpersonation();
                
                await impersonation.RunImpersonatedAsync(username, password, domain, async () =>
                {
                    await SaveFileAsync(folderPath, fullPath, request.File, ct);
                });
            }
            else
            {
                // SIN CREDENCIALES (punto de montaje local o acceso directo)
                await SaveFileAsync(folderPath, fullPath, request.File, ct);
            }

            // 6. Retornar respuesta exitosa
            var relativePathResult = $"/{relativePath}/{currentYear}/{sanitizedFileName}";
            
            return new FileUploadResponseDto
            {
                Success = true,
                Message = "File uploaded successfully.",
                FullPath = fullPath,
                RelativePath = relativePathResult,
                FileName = sanitizedFileName,
                FileSize = request.File.Length,
                Year = currentYear
            };
        }
        catch (PlatformNotSupportedException ex)
        {
            return CreateErrorResponse($"Platform not supported: {ex.Message}", request.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return CreateErrorResponse($"Authentication failed: {ex.Message}", request.FileName);
        }
        catch (Exception ex)
        {
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
            if (directory == null)
            {
                return null;
            }

            // 2. Sanitizar y construir ruta
            var sanitizedPath = filePath.TrimStart('/');
            var fullPath = Path.Combine(directory.PhysicalPath, sanitizedPath);

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

            if (fileBytes.Length == 0)
            {
                return null;
            }

            // 4. Determinar Content-Type
            var contentType = GetContentType(fullPath);
            var fileName = Path.GetFileName(fullPath);

            return (fileBytes, contentType, fileName);
        }
        catch
        {
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

            // 2. Sanitizar y construir ruta
            var sanitizedPath = filePath.TrimStart('/');
            var fullPath = Path.Combine(directory.PhysicalPath, sanitizedPath);

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
    /// Guarda un archivo en el sistema de archivos
    /// </summary>
    private static async Task SaveFileAsync(string folderPath, string fullPath, Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct)
    {
        // Crear carpeta si no existe
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Guardar archivo
        using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
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


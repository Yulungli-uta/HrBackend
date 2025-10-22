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
                return new FileUploadResponseDto
                {
                    Success = false,
                    Message = $"Directory with code '{request.DirectoryCode}' not found or inactive.",
                    FullPath = string.Empty,
                    RelativePath = string.Empty,
                    FileName = string.Empty,
                    FileSize = 0,
                    Year = 0
                };
            }

            // 2. Validar extensión del archivo
            var fileExtension = Path.GetExtension(request.FileName).ToLower();
            if (!string.IsNullOrEmpty(directory.Extension))
            {
                var allowedExtensions = directory.Extension.Split(',').Select(e => e.Trim().ToLower()).ToList();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return new FileUploadResponseDto
                    {
                        Success = false,
                        Message = $"File extension '{fileExtension}' is not allowed. Allowed extensions: {directory.Extension}",
                        FullPath = string.Empty,
                        RelativePath = string.Empty,
                        FileName = string.Empty,
                        FileSize = 0,
                        Year = 0
                    };
                }
            }

            // 3. Validar tamaño del archivo
            var fileSizeInMb = request.File.Length / (1024.0 * 1024.0);
            if (directory.MaxSizeMb.HasValue && fileSizeInMb > directory.MaxSizeMb.Value)
            {
                return new FileUploadResponseDto
                {
                    Success = false,
                    Message = $"File size ({fileSizeInMb:F2} MB) exceeds maximum allowed size ({directory.MaxSizeMb} MB).",
                    FullPath = string.Empty,
                    RelativePath = string.Empty,
                    FileName = string.Empty,
                    FileSize = 0,
                    Year = 0
                };
            }

            // 4. Obtener año actual
            int currentYear = DateTime.Now.Year;

            // 5. Construir ruta de carpeta con año
            var relativePath = request.RelativePath.TrimStart('/').TrimEnd('/');
            var folderPath = Path.Combine(directory.PhysicalPath, relativePath, currentYear.ToString());

            // 6. Sanitizar nombre del archivo (prevenir path traversal)
            var sanitizedFileName = Path.GetFileName(request.FileName);

            // 7. Construir ruta completa del archivo
            var fullPath = Path.Combine(folderPath, sanitizedFileName);

            // 8. Ejecutar operación con impersonation
            using (var impersonation = new WindowsImpersonation())
            {
                // Desencriptar credenciales
                var username = _encryptionService.Decrypt(_settings.NetworkCredentials.Username);
                var password = _encryptionService.Decrypt(_settings.NetworkCredentials.Password);
                var domain = string.IsNullOrEmpty(_settings.NetworkCredentials.Domain) 
                    ? null 
                    : _encryptionService.Decrypt(_settings.NetworkCredentials.Domain);

                // Iniciar impersonation
                impersonation.Impersonate(username, password, domain);

                // Verificar y crear carpeta si no existe
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Guardar el archivo
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream, ct);
                }
            }

            // 9. Construir ruta relativa para retornar
            var relativePathResult = $"/{relativePath}/{currentYear}/{sanitizedFileName}";

            // 10. Retornar respuesta exitosa
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
        catch (Exception ex)
        {
            return new FileUploadResponseDto
            {
                Success = false,
                Message = $"Error uploading file: {ex.Message}",
                FullPath = string.Empty,
                RelativePath = string.Empty,
                FileName = string.Empty,
                FileSize = 0,
                Year = 0
            };
        }
    }

    public async Task<List<FileUploadResponseDto>> UploadMultipleFilesAsync(FileUploadMultipleRequestDto request, CancellationToken ct = default)
    {
        var results = new List<FileUploadResponseDto>();

        if (request.Files == null || !request.Files.Any())
        {
            results.Add(new FileUploadResponseDto
            {
                Success = false,
                Message = "No files provided.",
                FullPath = string.Empty,
                RelativePath = string.Empty,
                FileName = string.Empty,
                FileSize = 0,
                Year = 0
            });
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

    public async Task<(byte[] fileBytes, string contentType, string fileName)?> DownloadFileAsync(string directoryCode, string filePath, CancellationToken ct = default)
    {
        try
        {
            // 1. Buscar DirectoryParameters por Code
            var directory = await _directoryService.GetByCodeAsync(directoryCode, ct);
            if (directory == null)
            {
                return null;
            }

            // 2. Sanitizar filePath (prevenir path traversal)
            var sanitizedPath = filePath.TrimStart('/');

            // 3. Construir ruta física completa
            var fullPath = Path.Combine(directory.PhysicalPath, sanitizedPath);

            byte[] fileBytes;
            
            // 4. Ejecutar operación con impersonation
            using (var impersonation = new WindowsImpersonation())
            {
                // Desencriptar credenciales
                var username = _encryptionService.Decrypt(_settings.NetworkCredentials.Username);
                var password = _encryptionService.Decrypt(_settings.NetworkCredentials.Password);
                var domain = string.IsNullOrEmpty(_settings.NetworkCredentials.Domain) 
                    ? null 
                    : _encryptionService.Decrypt(_settings.NetworkCredentials.Domain);

                // Iniciar impersonation
                impersonation.Impersonate(username, password, domain);

                // Verificar que el archivo existe
                if (!File.Exists(fullPath))
                {
                    return null;
                }

                // Leer el archivo
                fileBytes = await File.ReadAllBytesAsync(fullPath, ct);
            }

            // 5. Determinar Content-Type basado en la extensión
            var extension = Path.GetExtension(fullPath).ToLower();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                _ => "application/octet-stream"
            };

            // 6. Obtener nombre del archivo
            var fileName = Path.GetFileName(fullPath);

            return (fileBytes, contentType, fileName);
        }
        catch
        {
            return null;
        }
    }

    public async Task<FileDeleteResponseDto> DeleteFileAsync(string directoryCode, string filePath, CancellationToken ct = default)
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

            // 2. Sanitizar filePath (prevenir path traversal)
            var sanitizedPath = filePath.TrimStart('/');

            // 3. Construir ruta física completa
            var fullPath = Path.Combine(directory.PhysicalPath, sanitizedPath);

            // 4. Ejecutar operación con impersonation
            using (var impersonation = new WindowsImpersonation())
            {
                // Desencriptar credenciales
                var username = _encryptionService.Decrypt(_settings.NetworkCredentials.Username);
                var password = _encryptionService.Decrypt(_settings.NetworkCredentials.Password);
                var domain = string.IsNullOrEmpty(_settings.NetworkCredentials.Domain) 
                    ? null 
                    : _encryptionService.Decrypt(_settings.NetworkCredentials.Domain);

                // Iniciar impersonation
                impersonation.Impersonate(username, password, domain);

                // Verificar que el archivo existe
                if (!File.Exists(fullPath))
                {
                    return new FileDeleteResponseDto
                    {
                        Success = false,
                        Message = "File not found.",
                        FilePath = filePath
                    };
                }

                // Eliminar el archivo
                File.Delete(fullPath);
            }

            // 5. Retornar respuesta exitosa
            return new FileDeleteResponseDto
            {
                Success = true,
                Message = "File deleted successfully.",
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
}


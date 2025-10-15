using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

public class FileManagementService : IFileManagementService
{
    private readonly IDirectoryParametersService _directoryService;

    public FileManagementService(IDirectoryParametersService directoryService)
    {
        _directoryService = directoryService;
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

            // 6. Verificar y crear carpeta si no existe
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 7. Sanitizar nombre del archivo (prevenir path traversal)
            var sanitizedFileName = Path.GetFileName(request.FileName);

            // 8. Construir ruta completa del archivo
            var fullPath = Path.Combine(folderPath, sanitizedFileName);

            // 9. Guardar el archivo
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream, ct);
            }

            // 10. Construir ruta relativa para retornar
            var relativePathResult = $"/{relativePath}/{currentYear}/{sanitizedFileName}";

            // 11. Retornar respuesta exitosa
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

            // 4. Verificar que el archivo existe
            if (!File.Exists(fullPath))
            {
                return null;
            }

            // 5. Leer el archivo
            var fileBytes = await File.ReadAllBytesAsync(fullPath, ct);

            // 6. Determinar Content-Type basado en la extensión
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

            // 7. Obtener nombre del archivo
            var fileName = Path.GetFileName(fullPath);

            return (fileBytes, contentType, fileName);
        }
        catch
        {
            return null;
        }
    }
}


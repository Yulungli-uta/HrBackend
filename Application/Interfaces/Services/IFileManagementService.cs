using WsUtaSystem.Application.DTOs.FileManagement;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IFileManagementService
{
    Task<FileUploadResponseDto> UploadFileAsync(FileUploadRequestDto request, CancellationToken ct = default);
    Task<(byte[] fileBytes, string contentType, string fileName)?> DownloadFileAsync(string directoryCode, string filePath, CancellationToken ct = default);
    
    /// <summary>
    /// Sube múltiples archivos al directorio especificado con credenciales de red
    /// </summary>
    Task<List<FileUploadResponseDto>> UploadMultipleFilesAsync(FileUploadMultipleRequestDto request, CancellationToken ct = default);
    
    /// <summary>
    /// Elimina un archivo del directorio especificado con credenciales de red
    /// </summary>
    Task<FileDeleteResponseDto> DeleteFileAsync(string directoryCode, string filePath, CancellationToken ct = default);
}


using WsUtaSystem.Application.DTOs.FileManagement;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IFileManagementService
{
    Task<FileUploadResponseDto> UploadFileAsync(FileUploadRequestDto request, CancellationToken ct = default);
    Task<(byte[] fileBytes, string contentType, string fileName)?> DownloadFileAsync(string directoryCode, string filePath, CancellationToken ct = default);
}


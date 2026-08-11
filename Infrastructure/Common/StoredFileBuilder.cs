using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Common;

/// <summary>
/// Construye la fila de metadata <see cref="StoredFile"/> a partir del resultado de una
/// subida física ya exitosa. Compartido por todos los Services que implementan el patrón
/// "crear registro + documento" vía <see cref="TransactionalDocumentRepository"/>.
/// </summary>
public static class StoredFileBuilder
{
    public static StoredFile Build(
        string directoryCode,
        string entityType,
        string entityId,
        IFormFile file,
        FileUploadResponseDto physical,
        int? documentTypeId)
    {
        var (relativeFolder, storedFileName) = SplitFolderAndFile(physical.RelativePath);

        return new StoredFile
        {
            DirectoryCode = directoryCode,
            EntityType = entityType,
            EntityId = entityId,
            UploadYear = physical.Year,
            RelativeFolder = relativeFolder,
            StoredFileName = storedFileName,
            OriginalFileName = file.FileName,
            Extension = Path.GetExtension(storedFileName),
            ContentType = file.ContentType,
            SizeBytes = physical.FileSize,
            Status = 1,
            CreatedAt = DateTime.Now,
            DocumentTypeId = documentTypeId,
        };
    }

    public static (string relativeFolder, string storedFileName) SplitFolderAndFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return (string.Empty, "file");

        var sanitized = relativePath.Trim().TrimStart('/');
        var lastSlash = sanitized.LastIndexOf('/');
        if (lastSlash < 0) return (string.Empty, Path.GetFileName(sanitized));

        var folder = sanitized[..(lastSlash + 1)];
        var fileName = sanitized[(lastSlash + 1)..];
        return (folder, fileName);
    }
}

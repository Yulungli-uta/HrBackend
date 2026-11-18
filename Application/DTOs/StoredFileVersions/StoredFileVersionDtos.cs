namespace WsUtaSystem.Application.DTOs.StoredFileVersions;

/// <summary>Una versión de un archivo dentro de una ranura de un registro.</summary>
public sealed record StoredFileVersionDto(
    int VersionId,
    int VersionNo,
    int StoredFileId,
    Guid FileGuid,
    string? FileName,
    long SizeBytes,
    bool IsCurrent,
    int? UploadedBy,
    string? UploadedByName,
    DateTime UploadedAt,
    int? ReplacedBy,
    string? ReplacedByName,
    DateTime? ReplacedAt,
    string? Reason);

/// <summary>Reemplazo del documento firmado: el archivo ya subido y el motivo (obligatorio si ya había uno).</summary>
public sealed record ReplaceSignedDocumentRequest(int StoredFileId, string? Reason);

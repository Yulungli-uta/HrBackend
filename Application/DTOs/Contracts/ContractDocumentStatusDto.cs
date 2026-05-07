namespace WsUtaSystem.Application.DTOs.Contracts;

/// <summary>Estado del documento institucional vinculado a un contrato.</summary>
public sealed record ContractDocumentStatusDto(
    int ContractId,
    int? GeneratedDocumentId,
    int? TemplateVersionUsed,
    bool IsDocumentFrozen,
    string? DocumentStatus,
    string? FileName,
    int? StoredFileId
);

/// <summary>Request para congelar el documento de un contrato.</summary>
public sealed record FreezeDocumentRequest(int DocumentId, int TemplateVersion);

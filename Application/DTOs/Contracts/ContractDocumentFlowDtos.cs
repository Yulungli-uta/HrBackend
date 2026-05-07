namespace WsUtaSystem.Application.DTOs.Contracts
{
    public sealed record GenerateContractDocumentRequest(
    Dictionary<string, string>? Overrides = null,
    bool ForceRegenerate = false
);

    public sealed record GenerateContractDocumentResponse(
       int ContractID,
       int GeneratedDocumentId,
       string DocumentNumber,
       string FileName,
       string PdfBase64,
       int FileSizeBytes,
       bool IsDocumentFrozen,
       int ContractStatus,
       string ContractStatusName
   );

    public sealed record CreateContractResponse(
        ContractsDto Contract,
        GenerateContractDocumentResponse? Document
    );

    public sealed record UploadSignedContractDocumentRequest(
        int StoredFileId,
        string? Comment
    );

    public sealed record ContractDocumentCommentRequest(
        string? Comment
    );

    public sealed record CancelContractDocumentRequest(
        string Reason
    );
}

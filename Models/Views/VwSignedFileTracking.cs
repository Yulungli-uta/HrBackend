namespace WsUtaSystem.Models.Views;

/// <summary>Fila de <c>HR.vw_PersonnelActionSignedFile</c>: acción de personal activa con archivo firmado activo.</summary>
public sealed class VwPersonnelActionSignedFile
{
    public int ActionId { get; set; }
    public string? ActionNumber { get; set; }
    public int PersonId { get; set; }
    public string? PersonIdCard { get; set; }
    public string? PersonName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int ActionTypeId { get; set; }
    public string? ActionTypeName { get; set; }
    public string? Status { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? ElaboratorEmployeeId { get; set; }
    public string? ElaboratorIdCard { get; set; }
    public string? ElaboratorName { get; set; }
    public int SignedFileId { get; set; }
    public Guid SignedFileGuid { get; set; }
    public string? SignedFileName { get; set; }
    public long SignedFileSizeBytes { get; set; }
    public DateTime? SignedFileUploadedAt { get; set; }
}

/// <summary>Fila de <c>HR.vw_ContractSignedFile</c>: contrato activo con archivo firmado activo.</summary>
public sealed class VwContractSignedFile
{
    public int ContractID { get; set; }
    public string? ContractCode { get; set; }
    public int PersonId { get; set; }
    public string? PersonIdCard { get; set; }
    public string? PersonName { get; set; }
    public int DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int ContractTypeId { get; set; }
    public string? ContractTypeName { get; set; }
    public int StatusTypeId { get; set; }
    public string? StatusName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? CreatedByEmployeeId { get; set; }
    public string? CreatedByIdCard { get; set; }
    public string? CreatedByName { get; set; }
    public int SignedFileId { get; set; }
    public Guid SignedFileGuid { get; set; }
    public string? SignedFileName { get; set; }
    public long SignedFileSizeBytes { get; set; }
    public DateTime? SignedFileUploadedAt { get; set; }
}

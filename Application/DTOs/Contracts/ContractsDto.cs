namespace WsUtaSystem.Application.DTOs.Contracts;
public class ContractsDto
{
    public int ContractID { get; set; }

    public int? CertificationID { get; set; }
    public int? ParentID { get; set; }

    public string ContractCode { get; set; } = string.Empty;

    public int PersonID { get; set; }
    public int ContractTypeID { get; set; }

    public int? JobID { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public string? ContractFileName { get; set; }
    public string? ContractFilepath { get; set; }

    public int Status { get; set; }
    public string? ContractDescription { get; set; }

    public int DepartmentID { get; set; }
    public DateTime? AuthorizationDate { get; set; }

    public string? ResignationFileName { get; set; }
    public string? ResignationFilepath { get; set; }
    public string? ResignationCode { get; set; }

    public DateTime? RegResignationDate { get; set; }
    public DateTime? ResignationDate { get; set; }

    public string? CancelReason { get; set; }
    public string? CancelFilename { get; set; }
    public string? CancelFilepath { get; set; }
    public string? CancelCode { get; set; }

    public DateTime? RegistrationDateAnulCon { get; set; }

    public string? Nationality { get; set; }
    public string? Visa { get; set; }
    public string? Consulate { get; set; }
    public string? WorkOf { get; set; }

    public string? InicialContent { get; set; }
    public string? ResolucionContent { get; set; }

    public int? RelationshipType { get; set; }
    public string? Relationship { get; set; }

    public string? Competition { get; set; }
    public DateTime? CompetitionDate { get; set; }

    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int? GeneratedDocumentId { get; set; }
    public int? TemplateVersionUsed { get; set; }
    public bool IsDocumentFrozen { get; set; }

    public int? AuthorityNominatorId { get; set; }
    public int? DthDirectorId { get; set; }
    public bool IsDelegation { get; set; }

    /// <summary>Régimen laboral (LOSEP/LOES/CT). Auto-poblado desde la solicitud. Solo lectura.</summary>
    public int? LaborRegimeID { get; set; }
    public string? LaborRegimeName { get; set; }

    /// <summary>Modalidad de trabajo (TC/MT/Horas). Auto-poblado desde la solicitud. Solo lectura.</summary>
    public int? WorkModalityID { get; set; }
    public string? WorkModalityName { get; set; }

    /// <summary>Horas contratadas. Auto-poblado desde la solicitud. Solo lectura.</summary>
    public decimal? ContractedHours { get; set; }

    /// <summary>Sueldo real individual del contrato.</summary>
    public decimal? BaseSalary { get; set; }
}

/// <summary>
/// Solicitud para corregir un contrato ya existente, en cualquier estado (incluido VIGENTE).
/// A diferencia de <see cref="ContractsUpdateDto"/> vía <c>UpdateAsync</c> (solo BORRADOR/GENERADO),
/// exige un motivo obligatorio y queda registrada en HR.Audit (Action=CORRECTION).
/// </summary>
public sealed record CorrectContractRequest(
    string Reason,
    ContractsUpdateDto Data
);

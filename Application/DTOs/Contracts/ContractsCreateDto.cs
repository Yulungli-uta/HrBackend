namespace WsUtaSystem.Application.DTOs.Contracts;
public class ContractsCreateDto
{
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

    public int Status { get; set; } = 0;

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
    public bool GenerateDocument { get; set; } = false;

    public Dictionary<string, string>? DocumentOverrides { get; set; }

    public int? AuthorityNominatorId { get; set; }
    public int? DthDirectorId { get; set; }

    /// <summary>Indica si la firma se realiza por delegación (AuthorityNominatorId = el delegado).</summary>
    public bool IsDelegation { get; set; } = false;

    /// <summary>
    /// Régimen laboral. Si no se envía explícitamente, el servicio lo auto-puebla
    /// desde tbl_contractRequest vía CertificationID.
    /// </summary>
    public int? LaborRegimeID { get; set; }

    /// <summary>Modalidad de trabajo. Auto-poblado desde la solicitud si no se envía.</summary>
    public int? WorkModalityID { get; set; }

    /// <summary>Horas contratadas. Auto-poblado desde la solicitud si no se envía.</summary>
    public decimal? ContractedHours { get; set; }
}

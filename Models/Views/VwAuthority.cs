namespace WsUtaSystem.Models.Views;

/// <summary>
/// Modelo de solo lectura que mapea la vista HR.vw_Authority.
/// Combina autoridades de departamento con datos desnormalizados de persona, departamento,
/// tipo de autoridad y cargo para consultas eficientes sin joins adicionales.
/// </summary>
public class VwAuthority
{
    public int AuthorityID { get; set; }

    public int DepartmentID { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;

    public int EmployeeID { get; set; }
    public string EmployeeIDCard { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;

    public int AuthorityTypeID { get; set; }
    public string AuthorityTypeName { get; set; } = string.Empty;
    public string? AuthorityTypeDescription { get; set; }

    public int? JobID { get; set; }
    public string? JobDescription { get; set; }

    public string? Denomination { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? ResolutionCode { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

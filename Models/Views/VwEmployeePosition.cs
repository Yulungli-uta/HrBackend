namespace WsUtaSystem.Models.Views;

/// <summary>Posición vigente de un empleado (HR.vw_EmployeePositions): una fila por posición, no por persona.</summary>
public class VwEmployeePosition
{
    public int EmployeeID { get; set; }
    public int PersonID { get; set; }
    public string? IDCard { get; set; }
    public string? FullName { get; set; }
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
    /// <summary>Principal, Segundo régimen, Autoridad o Temporal.</summary>
    public string PositionType { get; set; } = null!;
    public int? LaborRegimeID { get; set; }
    public string? LaborRegimeName { get; set; }
    public int? JobID { get; set; }
    public string? JobName { get; set; }
    /// <summary>Cargo sin la dedicación (p. ej. "PROFESOR TITULAR AUXILIAR 1").</summary>
    public string? JobBaseName { get; set; }
    /// <summary>De dónde salió el cargo: FILA, TIPO_DE_CONTRATO, ESTRUCTURA_DOCENTE o FICHA.</summary>
    public string? JobSource { get; set; }
    public int? DepartmentID { get; set; }
    public string? DepartmentName { get; set; }
    public string? DedicationCode { get; set; }
    public string? DedicationName { get; set; }
    public string? DedicationSource { get; set; }
    public string? ModalityName { get; set; }
    public decimal? ContractedHours { get; set; }
    public string? DocumentNumber { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Source { get; set; }
}

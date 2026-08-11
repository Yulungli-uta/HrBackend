namespace WsUtaSystem.Models.Views;

/// <summary>
/// Mapea HR.vw_SiiesProfesores: un renglón por empleado docente con los datos crudos/homologados
/// necesarios para construir el reporte SIIES Profesores (matrices 5.2/5.3/5.4). Vista de solo
/// lectura, sin clave primaria. Las reglas condicionales SIIES se resuelven en
/// SiiesProfesoresReportSource, no en este modelo.
/// </summary>
/// <remarks>
/// Todas las columnas que provienen de un LEFT JOIN u OUTER APPLY (incluidas las que en su tabla
/// origen son NOT NULL, como <see cref="TeacherStructureID"/> o <see cref="RegimeIsActive"/>) se
/// mapean como nullable — un join externo puede producir NULL para toda la fila aunque la columna
/// base nunca sea NULL. Este fue exactamente el bug corregido en VwSiiesFuncionario.PuestoJerarquicoSuperior.
/// </remarks>
public class VwSiiesProfesor
{
    public int EmployeeID { get; set; }
    public int PersonID { get; set; }
    public int? IdentType { get; set; }
    public string? IdentTypeName { get; set; }
    public string IDCard { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly? BirthDate { get; set; }
    public string? SexSiiesLabel { get; set; }
    public string? GenderSiiesLabel { get; set; }
    public string? CountryId { get; set; }
    public string? EthnicitySiiesLabel { get; set; }
    public string? IndigenousNationalitySiiesLabel { get; set; }
    public string? DisabilitySiiesLabel { get; set; }
    public decimal? DisabilityPercentage { get; set; }
    public string? CONADISCard { get; set; }
    public string? InstitutionalEmail { get; set; }
    public string? DepartmentName { get; set; }

    // ── Vienen de OUTER APPLY sobre tbl_TeacherStructure: nullable aunque el registro exista ──
    public int? TeacherStructureID { get; set; }
    public decimal? WeeklyClassHours { get; set; }
    public string? TipoEscalafonNombramientoSiiesLabel { get; set; }
    public string? NivelSiiesLabel { get; set; }
    public string? CategoriaSiiesLabel { get; set; }
    public string? TiempoDedicacionSiiesLabel { get; set; }

    // ── Vienen de OUTER APPLY sobre tbl_EmployeeLaborRegime: nullable aunque el registro exista ──
    public string? DocumentNumber { get; set; }
    public string? RegimeDocumentType { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool? RegimeIsActive { get; set; }
    public bool? IngresoPorConcurso { get; set; }

    public string? RelacionIesSiiesLabel { get; set; }
    public decimal? ContractedHours { get; set; }

    // ── Vienen de tbl_Employees (tabla ancla del FROM): nunca nulas ──
    public bool EmployeeIsActive { get; set; }
    public DateOnly HireDate { get; set; }
}

namespace WsUtaSystem.Models.Views;

/// <summary>
/// Mapea HR.vw_SiiesFormacionProfesional: un renglón por título académico de un empleado docente
/// (matriz SIIES 5.5, Formación Profesional Terminado). Vista de solo lectura, sin clave primaria.
/// Solo incluye empleados que ya tienen un registro en tbl_TeacherStructure (INNER JOIN en la
/// vista) — hoy esa tabla está vacía, por lo que esta vista devuelve 0 filas hasta que se complete
/// la carga masiva planeada.
/// </summary>
public class VwSiiesFormacionProfesional
{
    public int EmployeeID { get; set; }
    public string IDCard { get; set; } = null!;
    public string? IdentTypeName { get; set; }
    public string? InstitutionCountryId { get; set; }
    public string? InstitutionName { get; set; }
    public string? NivelSiiesLabel { get; set; }
    public string? GradoSiiesLabel { get; set; }
    public string NombreTitulo { get; set; } = null!;
    public string? CampoDetalladoSiiesCode { get; set; }
    public string? SenescytRegistrationNumber { get; set; }
    public DateOnly? FechaObtuvoTitulo { get; set; }
}

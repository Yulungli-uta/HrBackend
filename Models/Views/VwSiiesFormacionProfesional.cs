namespace WsUtaSystem.Models.Views;

/// <summary>
/// Mapea HR.vw_SiiesFormacionProfesional: un renglón por título académico de un empleado docente
/// (matriz SIIES 5.5, Formación Profesional Terminado). Vista de solo lectura, sin clave primaria.
/// </summary>
/// <remarks>
/// 2026-09-11: se cambió de INNER JOIN sobre tbl_EducationLevels (tabla ancla) a LEFT JOIN desde
/// HR.vw_SiiesProfesores — antes, si un profesor no tenía ningún título cargado en
/// tbl_EducationLevels, ni siquiera su identificación aparecía y el reporte completo quedaba vacío
/// (0 títulos cargados hoy en todo el sistema para los profesores). Ahora aparecen todos los
/// profesores (Titulares + Ocasionales, mismo criterio que vw_SiiesProfesores), con los campos de
/// título en NULL cuando no tienen esa información cargada — visible para que RRHH sepa a quién le
/// falta cargar su Hoja de Vida académica, en vez de un reporte silenciosamente vacío.
/// </remarks>
public class VwSiiesFormacionProfesional
{
    public int EmployeeID { get; set; }
    public string IDCard { get; set; } = null!;
    public string? IdentTypeName { get; set; }

    /// <summary>Código SIIES de la IES (=InstitutionID), solo cuando es nacional. CODIGO_IES_ESTUDIO.</summary>
    public int? InstitutionSiiesCode { get; set; }

    /// <summary>Nombre del país de estudio (PAIS_ESTUDIO) - el instructivo exige el nombre, no un código. Renombrado 2026-09-19 (antes InstitutionCountryId, traía por error el código interno).</summary>
    public string? PaisEstudio { get; set; }
    public string? InstitutionName { get; set; }
    public string? NivelSiiesLabel { get; set; }
    public string? GradoSiiesLabel { get; set; }
    public string? NombreTitulo { get; set; }
    public string? CampoDetalladoSiiesCode { get; set; }
    public string? SenescytRegistrationNumber { get; set; }
    public DateOnly? FechaObtuvoTitulo { get; set; }

    // ── 2026-09-11: mismo período visible directo en la vista, ver VwSiiesProfesor. ──
    public string? LatestPeriodCode { get; set; }
    public DateOnly? LatestPeriodStart { get; set; }
    public DateOnly? LatestPeriodEnd { get; set; }
}

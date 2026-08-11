using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Application.DTOs.Reports.Common;

/// <summary>
/// Filtros comunes para todos los reportes.
/// </summary>
public record ReportFilterDto
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public int? DepartmentId { get; init; }
    //public int? FacultyId { get; init; }
    public int? EmployeeId { get; init; }
    public string? EmployeeType { get; init; }
    public bool? IsActive { get; init; }
    public bool? IncludeInactive { get; init; }
    public int? EmployeeTypeId { get; init; }

    /// <summary>
    /// Filtra por tipo de identificación ("CEDULA" o "PASAPORTE"). Usado por el reporte SIIES
    /// Funcionarios para segregar en un solo flujo las dos matrices (5.7/5.8) sin mezclarlas
    /// nunca en un mismo archivo. Null o vacío = CEDULA (matriz por defecto).
    /// </summary>
    public string? IdentType { get; init; }

    /// <summary>
    /// Filtra por número de identificación exacto (cédula o pasaporte), para generar el
    /// reporte de una sola persona. Null o vacío = todos los registros.
    /// </summary>
    public string? Identification { get; init; }

    /// <summary>
    /// Si es <c>true</c>, el PDF generado rota el texto de las cabeceras 90° (útil para
    /// reportes con muchas columnas angostas, ej. SIIES). Null o <c>false</c> = horizontal
    /// (comportamiento por defecto).
    /// </summary>
    public bool? VerticalHeaders { get; init; }

    /// <summary>
    /// Si es <c>false</c>, la cabecera del PDF aparece solo en la primera página en vez de
    /// repetirse en cada página. Null o <c>true</c> = se repite en cada página (por defecto).
    /// </summary>
    public bool? RepeatHeaderOnEveryPage { get; init; }

    /// <summary>Filtra por tipo de contrato (ID de tbl_ContractType). Null = todos.</summary>
    public int? ContractTypeId { get; init; }

    /// <summary>Filtra por régimen laboral (TypeId de ref_Types categoría LABOR_REGIME). Null = todos.</summary>
    public int? LaborRegimeId { get; init; }

    /// <summary>Filtra contratos por el empleado que los creó (ID de tbl_Employees). Null = todos.</summary>
    public int? CreatedByEmployeeId { get; init; }

    /// <summary>Filtra acciones de personal por tipo (ID de tbl_PersonnelActionTypes). Null = todos.</summary>
    public int? ActionTypeId { get; init; }

    /// <summary>Filtra por estado (ej: "VIGENTE", "FIRMADO_CARGADO", "ANULADO"). Null = todos.</summary>
    public string? Status { get; init; }

    /// <summary>Filtra por ubicación de servicio (ID de tbl_GuardServiceLocations). Null = todas.</summary>
    public int? LocationId { get; init; }

    /// <summary>Filtra por grupo de rotación de guardias (ID de tbl_GuardRotationGroups). Null = todos.</summary>
    public int? GroupId { get; init; }

    /// <summary>
    /// Filtra acciones de personal por categoría funcional.
    /// Valores: MOVEMENT, ENTRY, ECONOMIC, LEAVE, DISCIPLINARY, EXIT.
    /// Null o vacío = todas las categorías.
    /// </summary>
    public string[]? ActionCategories { get; init; }

    /// <summary>
    /// Orientación de página para el PDF generado.
    /// <para>
    /// Valores aceptados: <c>"portrait"</c> (vertical, por defecto) o <c>"landscape"</c> (horizontal).
    /// Si es <c>null</c>, el <see cref="Reports.Abstractions.IReportSource"/> usa su orientación predeterminada.
    /// </para>
    /// </summary>
    public string? Orientation { get; init; }

    /// <summary>
    /// Convierte el campo <see cref="Orientation"/> al enum <see cref="PageOrientation"/>.
    /// Devuelve <c>null</c> si no se especificó orientación (el source usará su default).
    /// </summary>
    public PageOrientation? GetPageOrientation() =>
        Orientation?.ToLowerInvariant() switch
        {
            "landscape" => PageOrientation.Landscape,
            "portrait"  => PageOrientation.Portrait,
            _           => null
        };
}

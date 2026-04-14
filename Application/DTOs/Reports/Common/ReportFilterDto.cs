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

namespace WsUtaSystem.Reports.Core;

/// <summary>
/// Orientación de página para el reporte PDF.
/// </summary>
public enum PageOrientation
{
    /// <summary>Vertical (A4 210×297 mm). Valor por defecto.</summary>
    Portrait = 0,

    /// <summary>Horizontal (A4 297×210 mm). Recomendado para reportes con muchas columnas.</summary>
    Landscape = 1
}

/// <summary>
/// Modelo genérico e inmutable que representa la definición completa de un reporte.
/// </summary>
/// <remarks>
/// <para>
/// Es el contrato de datos entre la capa de origen (<see cref="Abstractions.IReportSource"/>)
/// y la capa de renderizado (<see cref="Abstractions.IReportRenderer"/>).
/// Ninguno de los dos conoce la implementación del otro; solo comparten este modelo.
/// </para>
/// <para>
/// Principio ISP / DIP: los renderers dependen únicamente de esta abstracción,
/// no de DTOs específicos de empleados, asistencia, etc.
/// </para>
/// <para>
/// Las filas se representan como <c>IReadOnlyList&lt;IReadOnlyDictionary&lt;string, object?&gt;&gt;</c>
/// para evitar la proliferación de clases de fila específicas por reporte
/// (p.ej. <c>EmployeeReportRow</c>, <c>AttendanceReportRow</c>).
/// La clave del diccionario debe coincidir con <see cref="ReportColumn.Key"/>.
/// </para>
/// </remarks>
public sealed class ReportDefinition
{
    /// <summary>Título principal del reporte, mostrado en el encabezado.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Prefijo para el nombre del archivo generado.
    /// <see cref="ReportFileNameBuilder"/> lo combina con la fecha y extensión.
    /// Ejemplo: <c>"Reporte_Empleados"</c>.
    /// </summary>
    public required string FilePrefix { get; init; }

    /// <summary>
    /// Subtítulo o descripción adicional del reporte.
    /// Puede incluir el rango de fechas o filtros aplicados.
    /// </summary>
    public string Subtitle { get; init; } = string.Empty;

    /// <summary>
    /// Nombre del usuario que generó el reporte, usado en el pie de página.
    /// </summary>
    public string GeneratedBy { get; init; } = string.Empty;

    /// <summary>
    /// Fecha y hora de generación del reporte (UTC).
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Definición ordenada de las columnas del reporte.
    /// El orden de esta lista determina el orden de las columnas en PDF y Excel.
    /// </summary>
    public IReadOnlyList<ReportColumn> Columns { get; init; } = [];

    /// <summary>
    /// Filas de datos del reporte.
    /// Cada fila es un diccionario cuyas claves deben corresponder
    /// a los valores de <see cref="ReportColumn.Key"/> definidos en <see cref="Columns"/>.
    /// </summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];

    /// <summary>
    /// Orientación de página del reporte PDF.
    /// <para>
    /// Los renderers PDF deben respetar este valor al configurar el tamaño de página.
    /// El renderer Excel ignora esta propiedad (ajusta columnas automáticamente).
    /// </para>
    /// <para>
    /// Cada <see cref="Abstractions.IReportSource"/> establece la orientación
    /// según la cantidad de columnas de su reporte. El frontend puede sobreescribir
    /// este valor enviando el campo <c>orientation</c> en el filtro.
    /// </para>
    /// </summary>
    public PageOrientation Orientation { get; init; } = PageOrientation.Portrait;

    /// <summary>
    /// Número total de registros antes de aplicar paginación o límites.
    /// Útil para mostrar en el pie del reporte.
    /// </summary>
    public int TotalRecords => Rows.Count;
}

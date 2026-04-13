namespace WsUtaSystem.Reports.Core;

/// <summary>
/// Construye nombres de archivo para los reportes generados de forma centralizada y consistente.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: esta clase tiene una única responsabilidad — construir el nombre del archivo.
/// Ningún endpoint ni servicio debe construir el nombre de forma manual.
/// </para>
/// <para>
/// Ejemplos de salida:
/// <list type="bullet">
///   <item><description><c>Reporte_Empleados_20260413_101530.pdf</c></description></item>
///   <item><description><c>Reporte_Asistencia_20260413_101530.xlsx</c></description></item>
/// </list>
/// </para>
/// </remarks>
public static class ReportFileNameBuilder
{
    private static readonly IReadOnlyDictionary<ReportFormat, string> _extensions =
        new Dictionary<ReportFormat, string>
        {
            [ReportFormat.Pdf]   = "pdf",
            [ReportFormat.Excel] = "xlsx"
        };

    /// <summary>
    /// Construye el nombre del archivo usando el prefijo de la definición,
    /// la fecha/hora actual y la extensión correspondiente al formato.
    /// </summary>
    /// <param name="definition">Definición del reporte que contiene el <see cref="ReportDefinition.FilePrefix"/>.</param>
    /// <param name="format">Formato de exportación.</param>
    /// <returns>
    /// Nombre del archivo con timestamp en formato <c>{FilePrefix}_{yyyyMMdd_HHmmss}.{ext}</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Se lanza si el formato no tiene una extensión registrada.
    /// </exception>
    public static string Build(ReportDefinition definition, ReportFormat format)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!_extensions.TryGetValue(format, out var extension))
            throw new ArgumentException($"Formato de reporte no soportado: {format}", nameof(format));

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{definition.FilePrefix}_{timestamp}.{extension}";
    }

    /// <summary>
    /// Construye el nombre del archivo usando un prefijo explícito.
    /// Útil cuando no se dispone de una <see cref="ReportDefinition"/> completa.
    /// </summary>
    /// <param name="filePrefix">Prefijo del nombre del archivo (p.ej. <c>"Reporte_Empleados"</c>).</param>
    /// <param name="format">Formato de exportación.</param>
    /// <returns>Nombre del archivo con timestamp.</returns>
    public static string Build(string filePrefix, ReportFormat format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePrefix);

        if (!_extensions.TryGetValue(format, out var extension))
            throw new ArgumentException($"Formato de reporte no soportado: {format}", nameof(format));

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        return $"{filePrefix}_{timestamp}.{extension}";
    }

    /// <summary>
    /// Devuelve el tipo MIME correspondiente al formato de exportación.
    /// </summary>
    /// <param name="format">Formato de exportación.</param>
    /// <returns>Tipo MIME como cadena.</returns>
    public static string GetMimeType(ReportFormat format) => format switch
    {
        ReportFormat.Pdf   => "application/pdf",
        ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _                  => throw new ArgumentException($"Formato no soportado: {format}", nameof(format))
    };
}

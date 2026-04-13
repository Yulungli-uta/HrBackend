namespace WsUtaSystem.Reports.Core;

/// <summary>
/// Describe una columna dentro de un <see cref="ReportDefinition"/>.
/// </summary>
/// <remarks>
/// Se usa como tipo de valor inmutable (<c>record</c>) para garantizar
/// que la definición de columnas no sea modificada accidentalmente
/// una vez construida por un <c>IReportSource</c>.
/// </remarks>
/// <param name="Key">
/// Clave única que identifica la columna; debe coincidir con la clave
/// usada en el diccionario de cada fila de <see cref="ReportDefinition.Rows"/>.
/// </param>
/// <param name="Header">
/// Encabezado visible en el reporte (PDF o Excel).
/// </param>
/// <param name="Width">
/// Ancho relativo de la columna expresado en puntos o porcentaje,
/// según la implementación del renderer. Valor <c>0</c> indica ancho automático.
/// </param>
/// <param name="Alignment">
/// Alineación del contenido de la columna.
/// </param>
public sealed record ReportColumn(
    string Key,
    string Header,
    float Width = 0f,
    ColumnAlignment Alignment = ColumnAlignment.Left
);

/// <summary>
/// Alineación horizontal del contenido de una columna.
/// </summary>
public enum ColumnAlignment
{
    Left = 0,
    Center = 1,
    Right = 2
}

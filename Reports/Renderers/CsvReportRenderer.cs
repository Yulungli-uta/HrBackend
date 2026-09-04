using System.Globalization;
using System.Text;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Renderers;

/// <summary>
/// Renderizador genérico de reportes en formato CSV UTF-8 con separador punto y coma,
/// siguiendo las reglas generales del Instructivo Carga Masiva CACES (v2S, mayo 2026):
/// UTF-8, separador ';', fechas dd/MM/yyyy, decimales con coma, sin saltos de línea en celdas.
/// </summary>
/// <remarks>
/// Principio SRP: solo convierte una <see cref="ReportDefinition"/> en bytes CSV.
/// No conoce el origen de los datos de ningún reporte específico.
/// </remarks>
public sealed class CsvReportRenderer : IReportRenderer
{
    private static readonly CultureInfo DecimalCulture = new("es-EC");

    /// <inheritdoc/>
    public ReportFormat Format => ReportFormat.Csv;

    /// <inheritdoc/>
    public Task<byte[]> RenderAsync(ReportDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Columns is null || definition.Columns.Count == 0)
            throw new InvalidOperationException(
                $"La definición del reporte '{definition.Title}' no contiene columnas.");

        var sb = new StringBuilder();

        sb.AppendLine(string.Join(';', definition.Columns.Select(c => Escape(c.Header))));

        foreach (var row in definition.Rows)
        {
            var values = definition.Columns.Select(c =>
                row.TryGetValue(c.Key, out var value) ? FormatValue(value) : string.Empty);
            sb.AppendLine(string.Join(';', values.Select(Escape)));
        }

        // UTF-8 con BOM: el instructivo pide "CSV UTF-8" y tanto el Bloc de notas de Windows
        // como Excel necesitan el BOM para detectar la codificación correctamente (sin él,
        // Excel asume Windows-1252 y corrompe cualquier acento/ñ — bug real detectado 2026-08-27).
        // OJO: Encoding.GetBytes() NUNCA antepone el preamble/BOM, sin importar el valor de
        // encoderShouldEmitUTF8Identifier — ese flag solo afecta a GetPreamble(). Hay que usar
        // StreamWriter (que sí lo escribe automáticamente) o concatenar GetPreamble() a mano.
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            using (var writer = new StreamWriter(ms, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true))
                writer.Write(sb.ToString());
            bytes = ms.ToArray();
        }
        return Task.FromResult(bytes);
    }

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateOnly d => d.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        DateTime dt => dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        decimal dec => dec.ToString("0.##", DecimalCulture),
        double dbl => dbl.ToString("0.##", DecimalCulture),
        float f => f.ToString("0.##", DecimalCulture),
        bool b => b ? "SI" : "NO",
        _ => value.ToString() ?? string.Empty
    };

    /// <summary>Quita saltos de línea (prohibidos por el instructivo) y aplica comillas CSV solo si el valor lo requiere.</summary>
    private static string Escape(string value)
    {
        var clean = value.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();

        return clean.Contains(';') || clean.Contains('"')
            ? $"\"{clean.Replace("\"", "\"\"")}\""
            : clean;
    }
}

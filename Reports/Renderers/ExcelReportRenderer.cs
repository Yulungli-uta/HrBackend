using ClosedXML.Excel;
using WsUtaSystem.Application.Services.Reports.Configuration;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Renderers;

/// <summary>
/// Renderizador genérico de reportes en formato Excel (.xlsx) usando ClosedXML.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: esta clase tiene una única responsabilidad — convertir una
/// <see cref="ReportDefinition"/> en bytes de un archivo Excel. No conoce el origen
/// de los datos ni la lógica de negocio de ningún reporte específico.
/// </para>
/// <para>
/// Principio OCP: para agregar un nuevo reporte Excel no se modifica esta clase.
/// Solo se crea un nuevo <see cref="IReportSource"/> y se registra en DI.
/// </para>
/// <para>
/// Reutiliza la configuración de colores de <see cref="ReportConfiguration"/>
/// para mantener la identidad visual corporativa en todos los reportes.
/// </para>
/// </remarks>
public sealed class ExcelReportRenderer : IReportRenderer
{
    private readonly ReportConfiguration _config;
    private readonly ILogger<ExcelReportRenderer> _logger;

    /// <inheritdoc/>
    public ReportFormat Format => ReportFormat.Excel;

    public ExcelReportRenderer(
        ReportConfiguration config,
        ILogger<ExcelReportRenderer> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si la definición no contiene columnas válidas.
    /// </exception>
    public Task<byte[]> RenderAsync(ReportDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Columns is null || definition.Columns.Count == 0)
            throw new InvalidOperationException(
                $"La definición del reporte '{definition.Title}' no contiene columnas.");

        _logger.LogInformation(
            "ExcelReportRenderer: generando Excel para '{Title}' con {Rows} filas y {Cols} columnas.",
            definition.Title,
            definition.Rows?.Count ?? 0,
            definition.Columns.Count);

        using var workbook  = new XLWorkbook();
        var worksheet       = workbook.Worksheets.Add("Reporte");
        var currentRow      = 1;

        // ── Bloque de información del reporte ─────────────────────────────────
        currentRow = WriteReportInfo(worksheet, definition, currentRow);

        // ── Fila de cabecera de columnas ──────────────────────────────────────
        var headerRow = currentRow;
        WriteColumnHeaders(worksheet, definition, headerRow);
        currentRow++;

        // ── Filas de datos ────────────────────────────────────────────────────
        if (definition.Rows is not null && definition.Rows.Count > 0)
        {
            foreach (var row in definition.Rows)
            {
                WriteDataRow(worksheet, definition, row, currentRow);
                currentRow++;
            }
        }
        else
        {
            worksheet.Cell(currentRow, 1).Value = "No se encontraron registros para los filtros aplicados.";
            worksheet.Cell(currentRow, 1).Style.Font.Italic = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.Gray;
        }

        // ── Formato final ─────────────────────────────────────────────────────
        FinalizeWorksheet(worksheet, headerRow, definition.Columns.Count);

        // ── Serializar a bytes ────────────────────────────────────────────────
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();

        _logger.LogInformation(
            "ExcelReportRenderer: Excel generado correctamente. Tamaño: {Size} bytes.", bytes.Length);

        return Task.FromResult(bytes);
    }

    // ─── Métodos privados ─────────────────────────────────────────────────────

    /// <summary>
    /// Escribe el bloque de metadatos del reporte (título, subtítulo, generado por/en).
    /// </summary>
    /// <returns>La siguiente fila disponible después del bloque de información.</returns>
    private static int WriteReportInfo(IXLWorksheet worksheet, ReportDefinition definition, int startRow)
    {
        var row = startRow;

        // Título principal
        var titleCell = worksheet.Cell(row, 1);
        titleCell.Value = definition.Title;
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 14;
        row++;

        // Subtítulo (filtros aplicados)
        if (!string.IsNullOrWhiteSpace(definition.Subtitle))
        {
            var subtitleCell = worksheet.Cell(row, 1);
            subtitleCell.Value = definition.Subtitle;
            subtitleCell.Style.Font.Italic = true;
            subtitleCell.Style.Font.FontSize = 10;
            subtitleCell.Style.Font.FontColor = XLColor.Gray;
            row++;
        }

        // Metadatos: fecha de generación y usuario
        var metaCell = worksheet.Cell(row, 1);
        var generatedBy = string.IsNullOrWhiteSpace(definition.GeneratedBy)
            ? string.Empty
            : $" | Usuario: {definition.GeneratedBy}";
        metaCell.Value = $"Generado: {definition.GeneratedAt.ToLocalTime():dd/MM/yyyy HH:mm}{generatedBy}";
        metaCell.Style.Font.FontSize = 9;
        metaCell.Style.Font.Italic = true;
        metaCell.Style.Font.FontColor = XLColor.Gray;
        row++;

        // Fila separadora vacía
        row++;

        return row;
    }

    /// <summary>
    /// Escribe la fila de cabecera con los títulos de las columnas y aplica estilos corporativos.
    /// </summary>
    private void WriteColumnHeaders(IXLWorksheet worksheet, ReportDefinition definition, int headerRow)
    {
        for (var colIndex = 0; colIndex < definition.Columns.Count; colIndex++)
        {
            var col  = definition.Columns[colIndex];
            var cell = worksheet.Cell(headerRow, colIndex + 1);

            cell.Value = col.Header;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(_config.Colors.Primary);
            cell.Style.Font.FontColor       = XLColor.White;
            cell.Style.Font.Bold            = true;
            cell.Style.Alignment.Horizontal = col.Alignment switch
            {
                ColumnAlignment.Center => XLAlignmentHorizontalValues.Center,
                ColumnAlignment.Right  => XLAlignmentHorizontalValues.Right,
                _                      => XLAlignmentHorizontalValues.Left
            };
            cell.Style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
    }

    /// <summary>
    /// Escribe una fila de datos con el valor de cada columna y aplica estilos de cebra.
    /// </summary>
    private static void WriteDataRow(
        IXLWorksheet worksheet,
        ReportDefinition definition,
        IReadOnlyDictionary<string, object?> rowData,
        int rowNumber)
    {
        var isEven = (rowNumber % 2) == 0;

        for (var colIndex = 0; colIndex < definition.Columns.Count; colIndex++)
        {
            var col   = definition.Columns[colIndex];
            var cell  = worksheet.Cell(rowNumber, colIndex + 1);
            var value = rowData.TryGetValue(col.Key, out var v) ? v : null;

            // Asignar valor con tipo correcto para que Excel lo reconozca
            cell.Value = value switch
            {
                null                  => XLCellValue.FromObject(string.Empty),
                int    intVal         => intVal,
                long   longVal        => longVal,
                double doubleVal      => doubleVal,
                decimal decimalVal    => (double)decimalVal,
                float  floatVal       => (double)floatVal,
                bool   boolVal        => boolVal,
                DateTime dateVal      => dateVal,
                _                     => value.ToString() ?? string.Empty
            };

            // Estilo de cebra para filas pares
            if (isEven)
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");

            // Alineación según definición de columna
            cell.Style.Alignment.Horizontal = col.Alignment switch
            {
                ColumnAlignment.Center => XLAlignmentHorizontalValues.Center,
                ColumnAlignment.Right  => XLAlignmentHorizontalValues.Right,
                _                      => XLAlignmentHorizontalValues.Left
            };

            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
        }
    }

    /// <summary>
    /// Aplica formato final: auto-ajuste de columnas, congelado de cabecera y auto-filtro.
    /// </summary>
    private static void FinalizeWorksheet(IXLWorksheet worksheet, int headerRow, int columnCount)
    {
        // Auto-ajustar ancho de columnas
        worksheet.Columns().AdjustToContents();

        // Congelar filas hasta la cabecera de datos (inclusive)
        worksheet.SheetView.FreezeRows(headerRow);

        // Activar auto-filtro en la fila de cabecera
        var lastCell = worksheet.LastCellUsed();
        if (lastCell is not null && lastCell.Address.RowNumber >= headerRow)
        {
            worksheet
                .Range(headerRow, 1, lastCell.Address.RowNumber, columnCount)
                .SetAutoFilter();
        }
    }
}

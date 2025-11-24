using ClosedXML.Excel;
using WsUtaSystem.Application.Services.Reports.Configuration;

namespace WsUtaSystem.Application.Services.Reports.Generators.Base;

/// <summary>
/// Generador base para reportes Excel
/// </summary>
public class BaseExcelGenerator
{
    protected readonly ReportConfiguration _config;

    public BaseExcelGenerator(ReportConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Aplica estilos a la cabecera de una tabla
    /// </summary>
    public void ApplyHeaderStyle(IXLRange headerRange)
    {
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml(_config.Colors.Primary);
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    /// <summary>
    /// Aplica estilos a una fila de totales
    /// </summary>
    public void ApplyTotalRowStyle(IXLRange totalRange)
    {
        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml(_config.Colors.Secondary);
        totalRange.Style.Font.FontColor = XLColor.White;
        totalRange.Style.Font.Bold = true;
        totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
    }

    /// <summary>
    /// Aplica formato de moneda a un rango
    /// </summary>
    public void ApplyCurrencyFormat(IXLRange range)
    {
        range.Style.NumberFormat.Format = "$#,##0.00";
    }

    /// <summary>
    /// Aplica formato de fecha a un rango
    /// </summary>
    public void ApplyDateFormat(IXLRange range)
    {
        range.Style.NumberFormat.Format = "dd/mm/yyyy";
    }

    /// <summary>
    /// Aplica formato de fecha y hora a un rango
    /// </summary>
    public void ApplyDateTimeFormat(IXLRange range)
    {
        range.Style.NumberFormat.Format = "dd/mm/yyyy hh:mm";
    }

    /// <summary>
    /// Auto-ajusta columnas y activa filtros
    /// </summary>
    public void FinalizeWorksheet(IXLWorksheet worksheet)
    {
        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(1); // Congelar primera fila (cabecera)
        
        // Activar auto-filtro en la primera fila
        var lastCell = worksheet.LastCellUsed();
        if (lastCell != null)
        {
            var range = worksheet.Range(1, 1, lastCell.Address.RowNumber, lastCell.Address.ColumnNumber);
            range.SetAutoFilter();
        }
    }

    /// <summary>
    /// Agrega información del reporte en la hoja
    /// </summary>
    public void AddReportInfo(IXLWorksheet worksheet, string reportTitle, string userEmail)
    {
        worksheet.Cell(1, 1).Value = reportTitle;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        
        worksheet.Cell(2, 1).Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
        worksheet.Cell(2, 1).Style.Font.FontSize = 10;
        worksheet.Cell(2, 1).Style.Font.Italic = true;
        
        worksheet.Cell(3, 1).Value = $"Usuario: {userEmail}";
        worksheet.Cell(3, 1).Style.Font.FontSize = 10;
        worksheet.Cell(3, 1).Style.Font.Italic = true;
    }
}

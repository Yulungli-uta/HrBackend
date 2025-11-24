using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Services.Reports.Configuration;
using WsUtaSystem.Application.Services.Reports.Generators.Base;

namespace WsUtaSystem.Application.Services.Reports.Generators;

/// <summary>
/// Generador de reportes de asistencia
/// </summary>
public class AttendanceReportGenerator : BasePdfGenerator
{
    public AttendanceReportGenerator(ReportConfiguration config, IWebHostEnvironment env)
        : base(config, env)
    {
    }

    /// <summary>
    /// Genera reporte de asistencia en PDF
    /// </summary>
    public byte[] GeneratePdf(IEnumerable<AttendanceReportDto> attendances, ReportFilterDto filter, string userEmail)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin((float)_config.Margins.Top, Unit.Millimetre);
                
                page.Header().Element(c => ComposeHeader(c, "Reporte de Asistencia", filter, userEmail));
                page.Content().Element(c => ComposeContent(c, attendances));
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Genera reporte de asistencia en Excel
    /// </summary>
    public byte[] GenerateExcel(IEnumerable<AttendanceReportDto> attendances, string userEmail)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Asistencia");
        
        var excelGenerator = new BaseExcelGenerator(_config);
        excelGenerator.AddReportInfo(worksheet, "Reporte de Asistencia", userEmail);
        
        // Cabeceras
        worksheet.Cell(5, 1).Value = "Fecha";
        worksheet.Cell(5, 2).Value = "Empleado";
        worksheet.Cell(5, 3).Value = "Cédula";
        worksheet.Cell(5, 4).Value = "Departamento";
        worksheet.Cell(5, 5).Value = "Entrada";
        worksheet.Cell(5, 6).Value = "Salida";
        worksheet.Cell(5, 7).Value = "Horas Trabajadas";
        worksheet.Cell(5, 8).Value = "Tipo";
        worksheet.Cell(5, 9).Value = "Estado";

        excelGenerator.ApplyHeaderStyle(worksheet.Range(5, 1, 5, 9));

        // Datos
        int row = 6;
        decimal totalHours = 0;

        foreach (var att in attendances)
        {
            worksheet.Cell(row, 1).Value = att.AttendanceDate;
            worksheet.Cell(row, 2).Value = att.EmployeeName;
            worksheet.Cell(row, 3).Value = att.IdentificationNumber;
            worksheet.Cell(row, 4).Value = att.DepartmentName;
            worksheet.Cell(row, 5).Value = att.CheckIn?.ToString("HH:mm") ?? "N/A";
            worksheet.Cell(row, 6).Value = att.CheckOut?.ToString("HH:mm") ?? "N/A";
            worksheet.Cell(row, 7).Value = att.HoursWorked ?? 0;
            worksheet.Cell(row, 8).Value = att.AttendanceType;
            worksheet.Cell(row, 9).Value = att.Status;

            totalHours += att.HoursWorked ?? 0;
            row++;
        }

        // Fila de totales
        worksheet.Cell(row, 6).Value = "TOTAL HORAS:";
        worksheet.Cell(row, 6).Style.Font.Bold = true;
        worksheet.Cell(row, 7).Value = totalHours;
        
        excelGenerator.ApplyTotalRowStyle(worksheet.Range(row, 6, row, 7));
        excelGenerator.ApplyDateFormat(worksheet.Range(6, 1, row - 1, 1));
        
        excelGenerator.FinalizeWorksheet(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void ComposeContent(IContainer container, IEnumerable<AttendanceReportDto> attendances)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(70);  // Fecha
                    columns.RelativeColumn((float)2);   // Empleado
                    columns.RelativeColumn((float)1.5); // Departamento
                    columns.ConstantColumn(50);  // Entrada
                    columns.ConstantColumn(50);  // Salida
                    columns.ConstantColumn(50);  // Horas
                    columns.ConstantColumn(60);  // Estado
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Fecha").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Empleado").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Departamento").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Entrada").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Salida").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Horas").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Estado").FontSize(9).Bold();

                    IContainer CellStyle(IContainer c) => c
                        .Background(_config.Colors.Primary)
                        .Padding(5)
                        .AlignCenter()
                        .AlignMiddle();
                });

                int index = 0;
                foreach (var att in attendances)
                {
                    var bgColor = index % 2 == 0 ? _config.Colors.Background : _config.Colors.AlternateRow;
                    
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.AttendanceDate.ToString("dd/MM/yyyy")).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.EmployeeName).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.DepartmentName).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.CheckIn?.ToString("HH:mm") ?? "N/A").FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.CheckOut?.ToString("HH:mm") ?? "N/A").FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.HoursWorked?.ToString("F2") ?? "0.00").FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.Status).FontSize(8);
                    
                    index++;
                }

                IContainer DataCellStyle(IContainer c, string bgColor) => c
                    .Background(bgColor)
                    .BorderBottom((float)1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(5);
            });

            // Resumen
            var totalRecords = attendances.Count();
            var totalHours = attendances.Sum(a => a.HoursWorked ?? 0);
            var avgHours = totalRecords > 0 ? totalHours / totalRecords : 0;

            column.Item().PaddingTop((float)15).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Total de Registros: {totalRecords}").FontSize(10).Bold();
                    col.Item().Text($"Total Horas Trabajadas: {totalHours:F2}").FontSize(10).Bold();
                    col.Item().Text($"Promedio Horas/Día: {avgHours:F2}").FontSize(10);
                });
            });
        });
    }
}

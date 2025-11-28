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
public class AttendanceSumaryReportGenerator : BasePdfGenerator
{
    public AttendanceSumaryReportGenerator(ReportConfiguration config, IWebHostEnvironment env)
        : base(config, env)
    {
    }

    /// <summary>
    /// Genera reporte de asistencia en PDF
    /// </summary>
    public byte[] GeneratePdf(IEnumerable<AttendanceSumaryDto> attendances, ReportFilterDto filter, string userEmail)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape()); // Horizontal para más columnas
                page.Margin((float)_config.Margins.Top, Unit.Millimetre);

                // Cabecera
                page.Header().Element(c => ComposeHeader(c, "Reporte de Asistencia", filter, userEmail));

                // Contenido
                page.Content().Element(c => ComposeContent(c, attendances));

                // Pie de página
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Genera reporte de asistencia en Excel
    /// </summary>
    public byte[] GenerateExcel(IEnumerable<AttendanceSumaryDto> attendances, string userEmail)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Asistencia");

        // Información del reporte
        var excelGenerator = new BaseExcelGenerator(_config);
        excelGenerator.AddReportInfo(worksheet, "Reporte de Asistencia", userEmail);

        // Cabeceras
        worksheet.Cell(5, 1).Value = "ID Empleado";
        worksheet.Cell(5, 2).Value = "Cédula";
        worksheet.Cell(5, 3).Value = "Nombre";
        worksheet.Cell(5, 4).Value = "Tipo Empleado";
        worksheet.Cell(5, 5).Value = "Tipo Contrato";
        worksheet.Cell(5, 6).Value = "Fecha Trabajo";
        worksheet.Cell(5, 7).Value = "Min. Trabajados";
        worksheet.Cell(5, 8).Value = "Min. Regulares";
        worksheet.Cell(5, 9).Value = "Min. Extra";
        worksheet.Cell(5, 10).Value = "Min. Nocturnos";
        worksheet.Cell(5, 11).Value = "Min. Feriado";
        worksheet.Cell(5, 12).Value = "Min. Jornada";
        worksheet.Cell(5, 13).Value = "Min. Atrasos";
        worksheet.Cell(5, 14).Value = "Alimentación";
        worksheet.Cell(5, 15).Value = "Min. Justificación";

        // Aplicar estilo a cabecera
        excelGenerator.ApplyHeaderStyle(worksheet.Range(5, 1, 5, 15));

        // Datos
        int row = 6;
        int totalWorkedMinutes = 0;
        int totalRegularMinutes = 0;
        int totalOvertimeMinutes = 0;
        int totalNightMinutes = 0;
        int totalHolidayMinutes = 0;
        int totalTardinessMinutes = 0;
        decimal totalFoodSubsidy = 0;
        int totalJustificationMinutes = 0;

        foreach (var attendance in attendances)
        {
            worksheet.Cell(row, 1).Value = attendance.EmployeeID;
            worksheet.Cell(row, 2).Value = attendance.IDCard;
            worksheet.Cell(row, 3).Value = attendance.NombreCompleto;
            worksheet.Cell(row, 4).Value = attendance.EmployeeType;
            worksheet.Cell(row, 5).Value = attendance.ContractType ?? "N/A";
            worksheet.Cell(row, 6).Value = attendance.WorkDate;
            worksheet.Cell(row, 7).Value = attendance.TotalWorkedMinutes;
            worksheet.Cell(row, 8).Value = attendance.RegularMinutes;
            worksheet.Cell(row, 9).Value = attendance.OvertimeMinutes;
            worksheet.Cell(row, 10).Value = attendance.NightMinutes;
            worksheet.Cell(row, 11).Value = attendance.MinFeriado;
            worksheet.Cell(row, 12).Value = attendance.MinTotLaboral;
            worksheet.Cell(row, 13).Value = attendance.Atrazos;
            worksheet.Cell(row, 14).Value = attendance.Alimentacion;
            worksheet.Cell(row, 15).Value = attendance.MinJustificacion;

            totalWorkedMinutes += attendance.TotalWorkedMinutes;
            totalRegularMinutes += attendance.RegularMinutes;
            totalOvertimeMinutes += attendance.OvertimeMinutes;
            totalNightMinutes += attendance.NightMinutes;
            totalHolidayMinutes += attendance.MinFeriado;
            totalTardinessMinutes += attendance.Atrazos;
            totalFoodSubsidy += attendance.Alimentacion;
            totalJustificationMinutes += attendance.MinJustificacion;

            row++;
        }

        // Fila de totales
        worksheet.Cell(row, 6).Value = "TOTALES:";
        worksheet.Cell(row, 6).Style.Font.Bold = true;
        worksheet.Cell(row, 7).Value = totalWorkedMinutes;
        worksheet.Cell(row, 8).Value = totalRegularMinutes;
        worksheet.Cell(row, 9).Value = totalOvertimeMinutes;
        worksheet.Cell(row, 10).Value = totalNightMinutes;
        worksheet.Cell(row, 11).Value = totalHolidayMinutes;
        worksheet.Cell(row, 13).Value = totalTardinessMinutes;
        worksheet.Cell(row, 14).Value = totalFoodSubsidy;
        worksheet.Cell(row, 15).Value = totalJustificationMinutes;

        excelGenerator.ApplyTotalRowStyle(worksheet.Range(row, 6, row, 15));
        excelGenerator.ApplyCurrencyFormat(worksheet.Range(6, 14, row, 14));
        excelGenerator.ApplyDateFormat(worksheet.Range(6, 6, row - 1, 6));

        excelGenerator.FinalizeWorksheet(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void ComposeContent(IContainer container, IEnumerable<AttendanceSumaryDto> attendances)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // Tabla de asistencia
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(5);   // ID
                    columns.ConstantColumn(20);   // Cédula
                    columns.RelativeColumn(3f);   // Nombre
                    columns.ConstantColumn(35);   // Tipo Emp
                    columns.ConstantColumn(45);   // Tipo Contrato
                    columns.ConstantColumn(48);   // Fecha
                    columns.ConstantColumn(35);   // Min Trabajados
                    columns.ConstantColumn(35);   // Min Regulares
                    columns.ConstantColumn(35);   // Min Extra
                    columns.ConstantColumn(35);   // Min Nocturnos
                    columns.ConstantColumn(35);   // Min Feriado
                    columns.ConstantColumn(35);   // Min Jornada
                    columns.ConstantColumn(35);   // Atrasos
                    columns.ConstantColumn(40);   // Alimentación
                    columns.ConstantColumn(35);   // Min Justificación
                });

                // Cabecera
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("ID").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Cédula").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Nombre").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Fecha").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Min. Trab.").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Min. Reg.").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Min. Extra").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Min. Noct.").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Atrasos").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Aliment").FontSize(8).Bold();
                    header.Cell().Element(CellStyle).Text("Justif").FontSize(8).Bold();

                    IContainer CellStyle(IContainer c) => c
                        .Background(_config.Colors.Primary)
                        .Padding(4)
                        .AlignCenter()
                        .AlignMiddle();
                });

                // Filas de datos
                int index = 0;
                foreach (var att in attendances)
                {
                    var bgColor = index % 2 == 0 ? _config.Colors.Background : _config.Colors.AlternateRow;

                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.EmployeeID.ToString()).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.IDCard).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.NombreCompleto).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.WorkDate.ToString("dd/MM/yyyy")).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.TotalWorkedMinutes.ToString()).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.RegularMinutes.ToString()).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.OvertimeMinutes.ToString()).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.NightMinutes.ToString()).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.Atrazos.ToString()).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.Alimentacion.ToString()).FontSize(7);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(att.MinJustificacion.ToString()).FontSize(7);

                    index++;
                }

                IContainer DataCellStyle(IContainer c, string bgColor) => c
                    .Background(bgColor)
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(4);
            });

            // Resumen
            var totalRecords = attendances.Count();
            var totalWorkedMinutes = attendances.Sum(a => a.TotalWorkedMinutes);
            var totalRegularMinutes = attendances.Sum(a => a.RegularMinutes);
            var totalOvertimeMinutes = attendances.Sum(a => a.OvertimeMinutes);
            var totalNightMinutes = attendances.Sum(a => a.NightMinutes);
            var totalHolidayMinutes = attendances.Sum(a => a.MinFeriado);
            var totalTardinessMinutes = attendances.Sum(a => a.Atrazos);
            var totalFoodSubsidy = attendances.Sum(a => a.Alimentacion);
            var totalJustificationMinutes = attendances.Sum(a => a.MinJustificacion);

            column.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Total de Registros: {totalRecords}").FontSize(10).Bold();
                    col.Item().Text($"Total Minutos Trabajados: {totalWorkedMinutes:N0} ({ConvertMinutesToHours(totalWorkedMinutes)})").FontSize(8);
                    col.Item().Text($"Total Minutos Regulares: {totalRegularMinutes:N0} ({ConvertMinutesToHours(totalRegularMinutes)})").FontSize(8);
                    col.Item().Text($"Total Horas Extra: {totalOvertimeMinutes:N0} ({ConvertMinutesToHours(totalOvertimeMinutes)})").FontSize(8);
                    col.Item().Text($"Total Minutos Nocturnos: {totalNightMinutes:N0} ({ConvertMinutesToHours(totalNightMinutes)})").FontSize(8);
                    col.Item().Text($"Total Minutos Feriados: {totalHolidayMinutes:N0} ({ConvertMinutesToHours(totalHolidayMinutes)})").FontSize(8);
                    col.Item().Text($"Total Atrasos: {totalTardinessMinutes:N0} ({ConvertMinutesToHours(totalTardinessMinutes)})").FontSize(8);
                    col.Item().Text($"Total Minutos Justificados: {totalJustificationMinutes:N0} ({ConvertMinutesToHours(totalJustificationMinutes)})").FontSize(8);
                    col.Item().Text($"Total Subsidio Alimentación: ${totalFoodSubsidy:N2}").FontSize(10).Bold();
                });
            });
        });
    }

    /// <summary>
    /// Convierte minutos a formato de horas legible
    /// </summary>
    private string ConvertMinutesToHours(int minutes)
    {
        int hours = minutes / 60;
        int mins = minutes % 60;
        return $"{hours}h {mins}m";
    }
}
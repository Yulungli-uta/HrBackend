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
        var list = attendances?.ToList() ?? new List<AttendanceSumaryDto>();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Tamaño horizontal para muchas columnas (ya lo tenías, lo mantenemos)
                page.Size(PageSizes.A4.Landscape());

                // ✅ Márgenes bien aplicados (no solo Top)
                page.MarginTop(_config.Margins.Top, Unit.Millimetre);
                page.MarginBottom(_config.Margins.Bottom, Unit.Millimetre);
                page.MarginLeft(_config.Margins.Left, Unit.Millimetre);
                page.MarginRight(_config.Margins.Right, Unit.Millimetre);

                page.DefaultTextStyle(t => t.FontSize(8).FontColor(_config.Colors.TextPrimary));

                // Cabecera
                page.Header().Element(c => ComposeHeader(c, "Reporte de Asistencia", filter, userEmail));

                // Contenido
                page.Content().Element(c => ComposeContent(c, list));

                // Pie
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
        worksheet.Cell(5, 13).Value = "Atrasos";
        worksheet.Cell(5, 14).Value = "Alimentación";
        worksheet.Cell(5, 15).Value = "Min. Justificación";

        // Estilo cabecera
        excelGenerator.ApplyHeaderStyle(worksheet.Range(5, 1, 5, 15));

        int row = 6;
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

            row++;
        }

        // Ajustes finales
        excelGenerator.FinalizeWorksheet(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void ComposeContent(IContainer container, IReadOnlyList<AttendanceSumaryDto> attendances)
    {
        container.PaddingVertical(8).Column(column =>
        {
            column.Item().Table(table =>
            {
                // ✅ 15 columnas (todas relativas para evitar anchos inválidos)
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.6f); // ID
                    columns.RelativeColumn(1.2f); // Cédula
                    columns.RelativeColumn(2.6f); // Nombre
                    columns.RelativeColumn(1.0f); // Tipo Emp
                    columns.RelativeColumn(1.2f); // Tipo Contrato
                    columns.RelativeColumn(1.0f); // Fecha
                    columns.RelativeColumn(0.9f); // Min Trab
                    columns.RelativeColumn(0.9f); // Min Reg
                    columns.RelativeColumn(0.9f); // Min Extra
                    columns.RelativeColumn(0.9f); // Min Noct
                    columns.RelativeColumn(0.9f); // Min Feriado
                    columns.RelativeColumn(0.9f); // Min Jornada
                    columns.RelativeColumn(0.8f); // Atrasos
                    columns.RelativeColumn(0.9f); // Aliment
                    columns.RelativeColumn(0.9f); // Justif
                });

                // ✅ Header con 15 celdas
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("ID").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Cédula").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Nombre").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Tipo Emp.").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Tipo Contr.").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Fecha").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Min Trab.").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Min Reg.").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Min Extra").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Min Noct.").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Min Fer.").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Min Jor.").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Atrasos").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Aliment.").FontSize(8).Bold().FontColor(Colors.White);
                    header.Cell().Element(HeaderCell).Text("Justif.").FontSize(8).Bold().FontColor(Colors.White);

                    IContainer HeaderCell(IContainer c) => c
                        .Background(_config.Colors.Primary)
                        .PaddingVertical(3)
                        .PaddingHorizontal(2)
                        .AlignCenter()
                        .AlignMiddle();
                });

                // Filas de datos (✅ 15 celdas por fila)
                int index = 0;
                foreach (var att in attendances)
                {
                    var bgColor = index % 2 == 0 ? _config.Colors.Background : _config.Colors.AlternateRow;

                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.EmployeeID.ToString()).FontSize(7).AlignCenter();
                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.IDCard ?? "").FontSize(7);
                    table.Cell().Element(c => DataCell(c, bgColor))
                        .Text(att.NombreCompleto ?? "")
                        .FontSize(7)
                        .ClampLines(1);

                    table.Cell().Element(c => DataCell(c, bgColor))
                        .Text(MapEmployeeType(att.EmployeeType))
                        .FontSize(7)
                        .ClampLines(1);

                    table.Cell().Element(c => DataCell(c, bgColor))
                        .Text(string.IsNullOrWhiteSpace(att.ContractType) ? "N/A" : att.ContractType)
                        .FontSize(7)
                        .ClampLines(1);

                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.WorkDate.ToString("dd/MM/yyyy")).FontSize(7).AlignCenter();

                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.TotalWorkedMinutes.ToString()).FontSize(7).AlignRight();
                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.RegularMinutes.ToString()).FontSize(7).AlignRight();
                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.OvertimeMinutes.ToString()).FontSize(7).AlignRight();
                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.NightMinutes.ToString()).FontSize(7).AlignRight();
                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.MinFeriado.ToString()).FontSize(7).AlignRight();
                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.MinTotLaboral.ToString()).FontSize(7).AlignRight();
                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.Atrazos.ToString()).FontSize(7).AlignRight();
                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.Alimentacion.ToString("N2")).FontSize(7).AlignRight();
                    table.Cell().Element(c => DataCell(c, bgColor)).Text(att.MinJustificacion.ToString()).FontSize(7).AlignRight();

                    index++;
                }

                IContainer DataCell(IContainer c, string bg) => c
                    .Background(bg)
                    .Border(0.5f)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(2)
                    .PaddingHorizontal(2)
                    .AlignMiddle();
            });

            // Resumen (mantengo tu idea, pero completo con Jornada si quieres)
            var totalRecords = attendances.Count;
            var totalWorkedMinutes = attendances.Sum(a => a.TotalWorkedMinutes);
            var totalRegularMinutes = attendances.Sum(a => a.RegularMinutes);
            var totalOvertimeMinutes = attendances.Sum(a => a.OvertimeMinutes);
            var totalNightMinutes = attendances.Sum(a => a.NightMinutes);
            var totalHolidayMinutes = attendances.Sum(a => a.MinFeriado);
            var totalWorkdayMinutes = attendances.Sum(a => a.MinTotLaboral);
            var totalTardinessMinutes = attendances.Sum(a => a.Atrazos);
            var totalFoodSubsidy = attendances.Sum(a => a.Alimentacion);
            var totalJustificationMinutes = attendances.Sum(a => a.MinJustificacion);

            column.Item().PaddingTop(12).Column(col =>
            {
                col.Item().Text($"Total de Registros: {totalRecords}").FontSize(10).Bold();
                col.Item().Text($"Total Minutos Trabajados: {totalWorkedMinutes:N0} ({ConvertMinutesToHours(totalWorkedMinutes)})").FontSize(8);
                col.Item().Text($"Total Minutos Regulares: {totalRegularMinutes:N0} ({ConvertMinutesToHours(totalRegularMinutes)})").FontSize(8);
                col.Item().Text($"Total Horas Extra: {totalOvertimeMinutes:N0} ({ConvertMinutesToHours(totalOvertimeMinutes)})").FontSize(8);
                col.Item().Text($"Total Minutos Nocturnos: {totalNightMinutes:N0} ({ConvertMinutesToHours(totalNightMinutes)})").FontSize(8);
                col.Item().Text($"Total Minutos Feriados: {totalHolidayMinutes:N0} ({ConvertMinutesToHours(totalHolidayMinutes)})").FontSize(8);
                col.Item().Text($"Total Minutos Jornada: {totalWorkdayMinutes:N0} ({ConvertMinutesToHours(totalWorkdayMinutes)})").FontSize(8);
                col.Item().Text($"Total Atrasos: {totalTardinessMinutes:N0} ({ConvertMinutesToHours(totalTardinessMinutes)})").FontSize(8);
                col.Item().Text($"Total Minutos Justificados: {totalJustificationMinutes:N0} ({ConvertMinutesToHours(totalJustificationMinutes)})").FontSize(8);
                col.Item().Text($"Total Subsidio Alimentación: ${totalFoodSubsidy:N2}").FontSize(10).Bold();
            });
        });
    }

    private static string MapEmployeeType(int employeeType)
    {
        // Ajusta este mapeo según tu catálogo real
        return employeeType switch
        {
            1 => "Docente",
            2 => "Administrativo",
            3 => "Trabajador",
            _ => employeeType.ToString()
        };
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

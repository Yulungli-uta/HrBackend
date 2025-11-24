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
/// Generador de reportes de departamentos
/// </summary>
public class DepartmentReportGenerator : BasePdfGenerator
{
    public DepartmentReportGenerator(ReportConfiguration config, IWebHostEnvironment env)
        : base(config, env)
    {
    }

    /// <summary>
    /// Genera reporte de departamentos en PDF
    /// </summary>
    public byte[] GeneratePdf(IEnumerable<DepartmentReportDto> departments, ReportFilterDto filter, string userEmail)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin((float)_config.Margins.Top, Unit.Millimetre);
                
                page.Header().Element(c => ComposeHeader(c, "Reporte de Departamentos", filter, userEmail));
                page.Content().Element(c => ComposeContent(c, departments));
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Genera reporte de departamentos en Excel
    /// </summary>
    public byte[] GenerateExcel(IEnumerable<DepartmentReportDto> departments, string userEmail)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Departamentos");
        
        var excelGenerator = new BaseExcelGenerator(_config);
        excelGenerator.AddReportInfo(worksheet, "Reporte de Departamentos", userEmail);
        
        // Cabeceras
        worksheet.Cell(5, 1).Value = "ID";
        worksheet.Cell(5, 2).Value = "Departamento";
        worksheet.Cell(5, 3).Value = "Código";
        worksheet.Cell(5, 4).Value = "Facultad";
        worksheet.Cell(5, 5).Value = "Estado";
        worksheet.Cell(5, 6).Value = "Total Empleados";
        worksheet.Cell(5, 7).Value = "Activos";
        worksheet.Cell(5, 8).Value = "Inactivos";
        worksheet.Cell(5, 9).Value = "Salario Promedio";
        worksheet.Cell(5, 10).Value = "Total Salarios";

        excelGenerator.ApplyHeaderStyle(worksheet.Range(5, 1, 5, 10));

        // Datos
        int row = 6;
        int totalEmployees = 0;
        int totalActive = 0;
        decimal grandTotalSalaries = 0;

        foreach (var dept in departments)
        {
            worksheet.Cell(row, 1).Value = dept.Id;
            worksheet.Cell(row, 2).Value = dept.DepartmentName;
            worksheet.Cell(row, 3).Value = dept.DepartmentCode;
            worksheet.Cell(row, 4).Value = dept.FacultyName;
            worksheet.Cell(row, 5).Value = dept.IsActive ? "Activo" : "Inactivo";
            worksheet.Cell(row, 6).Value = dept.TotalEmployees;
            worksheet.Cell(row, 7).Value = dept.ActiveEmployees;
            worksheet.Cell(row, 8).Value = dept.InactiveEmployees;
            worksheet.Cell(row, 9).Value = dept.AverageSalary;
            worksheet.Cell(row, 10).Value = dept.TotalSalaries;

            totalEmployees += dept.TotalEmployees;
            totalActive += dept.ActiveEmployees;
            grandTotalSalaries += dept.TotalSalaries;
            
            row++;
        }

        // Fila de totales
        worksheet.Cell(row, 5).Value = "TOTALES:";
        worksheet.Cell(row, 5).Style.Font.Bold = true;
        worksheet.Cell(row, 6).Value = totalEmployees;
        worksheet.Cell(row, 7).Value = totalActive;
        worksheet.Cell(row, 10).Value = grandTotalSalaries;
        
        excelGenerator.ApplyTotalRowStyle(worksheet.Range(row, 5, row, 10));
        excelGenerator.ApplyCurrencyFormat(worksheet.Range(6, 9, row, 10));
        
        excelGenerator.FinalizeWorksheet(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void ComposeContent(IContainer container, IEnumerable<DepartmentReportDto> departments)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);  // ID
                    columns.RelativeColumn((float)2);   // Departamento
                    columns.RelativeColumn((float)1.5); // Facultad
                    columns.ConstantColumn(50);  // Total Emp
                    columns.ConstantColumn(50);  // Activos
                    columns.ConstantColumn(80);  // Salario Prom
                    columns.ConstantColumn(90);  // Total Sal
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("ID").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Departamento").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Facultad").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Total").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Activos").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Sal. Prom.").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Total Sal.").FontSize(9).Bold();

                    IContainer CellStyle(IContainer c) => c
                        .Background(_config.Colors.Primary)
                        .Padding(5)
                        .AlignCenter()
                        .AlignMiddle();
                });

                int index = 0;
                foreach (var dept in departments)
                {
                    var bgColor = index % 2 == 0 ? _config.Colors.Background : _config.Colors.AlternateRow;
                    
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(dept.Id.ToString()).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(dept.DepartmentName).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(dept.FacultyName).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(dept.TotalEmployees.ToString()).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(dept.ActiveEmployees.ToString()).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text($"${dept.AverageSalary:N2}").FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text($"${dept.TotalSalaries:N2}").FontSize(8);
                    
                    index++;
                }

                IContainer DataCellStyle(IContainer c, string bgColor) => c
                    .Background(bgColor)
                    .BorderBottom((float)1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(5);
            });

            // Resumen
            var totalDepts = departments.Count();
            var totalEmployees = departments.Sum(d => d.TotalEmployees);
            var grandTotalSalaries = departments.Sum(d => d.TotalSalaries);

            column.Item().PaddingTop((float)15).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Total de Departamentos: {totalDepts}").FontSize(10).Bold();
                    col.Item().Text($"Total de Empleados: {totalEmployees}").FontSize(10).Bold();
                    col.Item().Text($"Total en Salarios: ${grandTotalSalaries:N2}").FontSize(10).Bold();
                });
            });
        });
    }
}

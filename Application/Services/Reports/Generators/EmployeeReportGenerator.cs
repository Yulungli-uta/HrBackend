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
/// Generador de reportes de empleados
/// </summary>
public class EmployeeReportGenerator : BasePdfGenerator
{
    public EmployeeReportGenerator(ReportConfiguration config, IWebHostEnvironment env)
        : base(config, env)
    {
    }

    /// <summary>
    /// Genera reporte de empleados en PDF
    /// </summary>
    public byte[] GeneratePdf(IEnumerable<EmployeeReportDto> employees, ReportFilterDto filter, string userEmail)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape()); // Horizontal para más columnas
                page.Margin((float)_config.Margins.Top, Unit.Millimetre);
                
                // Cabecera
                page.Header().Element(c => ComposeHeader(c, "Reporte de Empleados", filter, userEmail));
                
                // Contenido
                page.Content().Element(c => ComposeContent(c, employees));
                
                // Pie de página
                page.Footer().Element(c => ComposeFooter(c));
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Genera reporte de empleados en Excel
    /// </summary>
    public byte[] GenerateExcel(IEnumerable<EmployeeReportDto> employees, string userEmail)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Empleados");
        
        // Información del reporte
        var excelGenerator = new BaseExcelGenerator(_config);
        excelGenerator.AddReportInfo(worksheet, "Reporte de Empleados", userEmail);
        
        // Cabeceras
        worksheet.Cell(5, 1).Value = "ID";
        worksheet.Cell(5, 2).Value = "Nombre Completo";
        worksheet.Cell(5, 3).Value = "Cédula";
        worksheet.Cell(5, 4).Value = "Email";
        worksheet.Cell(5, 5).Value = "Departamento";
        worksheet.Cell(5, 6).Value = "Facultad";
        worksheet.Cell(5, 7).Value = "Tipo";
        worksheet.Cell(5, 8).Value = "Estado";
        worksheet.Cell(5, 9).Value = "Salario Base";
        worksheet.Cell(5, 10).Value = "Salario Neto";
        worksheet.Cell(5, 11).Value = "Tipo Contrato";
        worksheet.Cell(5, 12).Value = "Fecha Contratación";

        // Aplicar estilo a cabecera
        excelGenerator.ApplyHeaderStyle(worksheet.Range(5, 1, 5, 12));

        // Datos
        int row = 6;
        decimal totalBaseSalary = 0;
        decimal totalNetSalary = 0;

        foreach (var emp in employees)
        {
            worksheet.Cell(row, 1).Value = emp.Id;
            worksheet.Cell(row, 2).Value = emp.FullName;
            worksheet.Cell(row, 3).Value = emp.IdentificationNumber;
            worksheet.Cell(row, 4).Value = emp.Email;
            worksheet.Cell(row, 5).Value = emp.DepartmentName;
            worksheet.Cell(row, 6).Value = emp.FacultyName;
            worksheet.Cell(row, 7).Value = emp.EmployeeType;
            worksheet.Cell(row, 8).Value = emp.IsActive ? "Activo" : "Inactivo";
            worksheet.Cell(row, 9).Value = emp.BaseSalary;
            worksheet.Cell(row, 10).Value = emp.NetSalary;
            worksheet.Cell(row, 11).Value = emp.ContractType ?? "N/A";
            worksheet.Cell(row, 12).Value = emp.HireDate;

            totalBaseSalary += emp.BaseSalary;
            totalNetSalary += emp.NetSalary;
            
            row++;
        }

        // Fila de totales
        worksheet.Cell(row, 8).Value = "TOTALES:";
        worksheet.Cell(row, 8).Style.Font.Bold = true;
        worksheet.Cell(row, 9).Value = totalBaseSalary;
        worksheet.Cell(row, 10).Value = totalNetSalary;
        
        excelGenerator.ApplyTotalRowStyle(worksheet.Range(row, 8, row, 10));
        excelGenerator.ApplyCurrencyFormat(worksheet.Range(6, 9, row, 10));
        excelGenerator.ApplyDateFormat(worksheet.Range(6, 12, row - 1, 12));
        
        excelGenerator.FinalizeWorksheet(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void ComposeContent(IContainer container, IEnumerable<EmployeeReportDto> employees)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // Tabla de empleados
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);  // ID
                    columns.RelativeColumn((float)2);   // Nombre
                    columns.RelativeColumn((float)1);   // Cédula
                    columns.RelativeColumn((float)1.5); // Departamento
                    columns.RelativeColumn((float)1);   // Tipo
                    columns.ConstantColumn(50);  // Estado
                    columns.ConstantColumn(80);  // Salario
                });

                // Cabecera
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("ID").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Nombre Completo").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Cédula").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Departamento").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Tipo").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Estado").FontSize(9).Bold();
                    header.Cell().Element(CellStyle).Text("Salario Base").FontSize(9).Bold();

                    IContainer CellStyle(IContainer c) => c
                        .Background(_config.Colors.Primary)
                        .Padding(5)
                        .AlignCenter()
                        .AlignMiddle();
                });

                // Filas de datos
                int index = 0;
                foreach (var emp in employees)
                {
                    var bgColor = index % 2 == 0 ? _config.Colors.Background : _config.Colors.AlternateRow;
                    
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(emp.Id.ToString()).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(emp.FullName).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(emp.IdentificationNumber).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(emp.DepartmentName).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(emp.EmployeeType).FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text(emp.IsActive ? "Activo" : "Inactivo").FontSize(8);
                    table.Cell().Element(c => DataCellStyle(c, bgColor)).Text($"${emp.BaseSalary:N2}").FontSize(8);
                    
                    index++;
                }

                IContainer DataCellStyle(IContainer c, string bgColor) => c
                    .Background(bgColor)
                    .BorderBottom((float)1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(5);
            });

            // Resumen
            var totalEmployees = employees.Count();
            var activeEmployees = employees.Count(e => e.IsActive);
            var totalSalaries = employees.Sum(e => e.BaseSalary);

            column.Item().PaddingTop((float)15).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Total de Empleados: {totalEmployees}").FontSize(10).Bold();
                    col.Item().Text($"Empleados Activos: {activeEmployees}").FontSize(10);
                    col.Item().Text($"Total Salarios: ${totalSalaries:N2}").FontSize(10).Bold();
                });
            });
        });
    }
}

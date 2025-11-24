using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Reports;

namespace WsUtaSystem.Endpoints;

/// <summary>
/// Endpoints de reportes
/// </summary>
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/rh/reports")
            .WithTags("Reports")
            .RequireAuthorization();

        // ==================== EMPLEADOS ====================
        
        group.MapPost("/employees/pdf/preview", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateEmployeesPdfAsync(filter, context);
            return Results.File(pdfBytes, "application/pdf", "Reporte_Empleados_Preview.pdf");
        })
        .WithName("PreviewEmployeesPdf")
        .WithSummary("Vista previa del reporte de empleados en PDF")
        .Produces<FileResult>(200, "application/pdf");

        group.MapPost("/employees/pdf/download", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateEmployeesPdfAsync(filter, context);
            var fileName = $"Reporte_Empleados_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return Results.File(pdfBytes, "application/pdf", fileName);
        })
        .WithName("DownloadEmployeesPdf")
        .WithSummary("Descarga directa del reporte de empleados en PDF")
        .Produces<FileResult>(200, "application/pdf");

        group.MapPost("/employees/excel", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var excelBytes = await reportService.GenerateEmployeesExcelAsync(filter, context);
            var fileName = $"Reporte_Empleados_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return Results.File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        })
        .WithName("DownloadEmployeesExcel")
        .WithSummary("Descarga del reporte de empleados en Excel")
        .Produces<FileResult>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // ==================== ASISTENCIA ====================
        
        group.MapPost("/attendance/pdf/preview", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateAttendancePdfAsync(filter, context);
            return Results.File(pdfBytes, "application/pdf", "Reporte_Asistencia_Preview.pdf");
        })
        .WithName("PreviewAttendancePdf")
        .WithSummary("Vista previa del reporte de asistencia en PDF")
        .Produces<FileResult>(200, "application/pdf");

        group.MapPost("/attendance/pdf/download", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateAttendancePdfAsync(filter, context);
            var fileName = $"Reporte_Asistencia_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return Results.File(pdfBytes, "application/pdf", fileName);
        })
        .WithName("DownloadAttendancePdf")
        .WithSummary("Descarga directa del reporte de asistencia en PDF")
        .Produces<FileResult>(200, "application/pdf");

        group.MapPost("/attendance/excel", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var excelBytes = await reportService.GenerateAttendanceExcelAsync(filter, context);
            var fileName = $"Reporte_Asistencia_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return Results.File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        })
        .WithName("DownloadAttendanceExcel")
        .WithSummary("Descarga del reporte de asistencia en Excel")
        .Produces<FileResult>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // ==================== DEPARTAMENTOS ====================
        
        group.MapPost("/departments/pdf/preview", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateDepartmentsPdfAsync(filter, context);
            return Results.File(pdfBytes, "application/pdf", "Reporte_Departamentos_Preview.pdf");
        })
        .WithName("PreviewDepartmentsPdf")
        .WithSummary("Vista previa del reporte de departamentos en PDF")
        .Produces<FileResult>(200, "application/pdf");

        group.MapPost("/departments/pdf/download", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateDepartmentsPdfAsync(filter, context);
            var fileName = $"Reporte_Departamentos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return Results.File(pdfBytes, "application/pdf", fileName);
        })
        .WithName("DownloadDepartmentsPdf")
        .WithSummary("Descarga directa del reporte de departamentos en PDF")
        .Produces<FileResult>(200, "application/pdf");

        group.MapPost("/departments/excel", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var excelBytes = await reportService.GenerateDepartmentsExcelAsync(filter, context);
            var fileName = $"Reporte_Departamentos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return Results.File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        })
        .WithName("DownloadDepartmentsExcel")
        .WithSummary("Descarga del reporte de departamentos en Excel")
        .Produces<FileResult>(200, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // ==================== AUDITORÍA ====================
        
        group.MapGet("/audits", async (
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? reportType,
            [FromQuery] Guid? userId,
            [FromQuery] int top,
            [FromServices] IReportAuditService auditService) =>
        {
            var audits = await auditService.GetAuditsAsync(startDate, endDate, reportType, userId, top);
            return Results.Ok(audits);
        })
        .WithName("GetReportAudits")
        .WithSummary("Obtiene el historial de auditoría de reportes generados")
        .Produces<IEnumerable<ReportAuditDto>>(200);
    }
}

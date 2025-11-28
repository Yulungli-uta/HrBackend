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
        var group = app.MapGroup("/reports")
            .WithTags("Reports");
            //.RequireAuthorization();

        // ==================== EMPLEADOS ====================

        group.MapPost("/employees/preview", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            Console.WriteLine("******************************************Generating employee report preview...");
            var pdfBytes = await reportService.GenerateEmployeesPdfAsync(filter, context);

            if (pdfBytes is null || pdfBytes.Length == 0)
            {
                var errorResponse = new PreviewResponseDto(
                    Status: "error",
                    Data: null,
                    Error: new PreviewErrorDto("No se pudo generar el reporte de empleados.")
                );
                //context.Response.DisableCache();
                return Results.Ok(errorResponse);
            }

            var base64 = Convert.ToBase64String(pdfBytes);

            var data = new PdfPreviewDataDto(
                Base64Data: base64,
                FileName: "Reporte_Empleados_Preview.pdf",
                MimeType: "application/pdf"
            );

            var response = new PreviewResponseDto(
                Status: "success",
                Data: data,
                Error: null
            );

            //context.Response.DisableCache();
            return Results.Ok(response);
        })
        .WithName("PreviewEmployeesPdf")
        .WithSummary("Vista previa del reporte de empleados en PDF")
        .Produces<PreviewResponseDto>(StatusCodes.Status200OK, "application/json");

        group.MapPost("/employees/pdf/download", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateEmployeesPdfAsync(filter, context);
            var fileName = $"Reporte_Empleados_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            //context.Response.DisableCache();
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            return Results.File(pdfBytes, "application/pdf", fileName);
        })
        .WithName("DownloadEmployeesPdf")
        .WithSummary("Descarga directa del reporte de empleados en PDF")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapPost("/employees/excel", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var excelBytes = await reportService.GenerateEmployeesExcelAsync(filter, context);
            var fileName = $"Reporte_Empleados_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            //context.Response.DisableCache();
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            return Results.File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        })
        .WithName("DownloadEmployeesExcel")
        .WithSummary("Descarga del reporte de empleados en Excel")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // ==================== ASISTENCIA ====================

        group.MapPost("/attendance/preview", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateAttendancePdfAsync(filter, context);

            if (pdfBytes is null || pdfBytes.Length == 0)
            {
                var errorResponse = new PreviewResponseDto(
                    Status: "error",
                    Data: null,
                    Error: new PreviewErrorDto("No se pudo generar el reporte de asistencia.")
                );
                //context.Response.DisableCache();
                return Results.Ok(errorResponse);
            }

            var base64 = Convert.ToBase64String(pdfBytes);

            var data = new PdfPreviewDataDto(
                Base64Data: base64,
                FileName: "Reporte_Asistencia_Preview.pdf",
                MimeType: "application/pdf"
            );

            var response = new PreviewResponseDto(
                Status: "success",
                Data: data,
                Error: null
            );

            //context.Response.DisableCache();
            return Results.Ok(response);
        })
        .WithName("PreviewAttendancePdf")
        .WithSummary("Vista previa del reporte de asistencia en PDF")
        .Produces<PreviewResponseDto>(StatusCodes.Status200OK, "application/json");

        group.MapPost("/attendance/pdf/download", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateAttendancePdfAsync(filter, context);
            var fileName = $"Reporte_Asistencia_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            //context.Response.DisableCache();
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            return Results.File(pdfBytes, "application/pdf", fileName);
        })
        .WithName("DownloadAttendancePdf")
        .WithSummary("Descarga directa del reporte de asistencia en PDF")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapPost("/attendance/excel", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var excelBytes = await reportService.GenerateAttendanceExcelAsync(filter, context);
            var fileName = $"Reporte_Asistencia_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            //context.Response.DisableCache();
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            return Results.File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        })
        .WithName("DownloadAttendanceExcel")
        .WithSummary("Descarga del reporte de asistencia en Excel")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // ==================== DEPARTAMENTOS ====================

        group.MapPost("/departments/preview", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateDepartmentsPdfAsync(filter, context);

            if (pdfBytes is null || pdfBytes.Length == 0)
            {
                var errorResponse = new PreviewResponseDto(
                    Status: "error",
                    Data: null,
                    Error: new PreviewErrorDto("No se pudo generar el reporte de departamentos.")
                );
                //context.Response.DisableCache();
                return Results.Ok(errorResponse);
            }

            var base64 = Convert.ToBase64String(pdfBytes);

            var data = new PdfPreviewDataDto(
                Base64Data: base64,
                FileName: "Reporte_Departamentos_Preview.pdf",
                MimeType: "application/pdf"
            );

            var response = new PreviewResponseDto(
                Status: "success",
                Data: data,
                Error: null
            );

            //context.Response.DisableCache();
            return Results.Ok(response);
        })
        .WithName("PreviewDepartmentsPdf")
        .WithSummary("Vista previa del reporte de departamentos en PDF")
        .Produces<PreviewResponseDto>(StatusCodes.Status200OK, "application/json");

        group.MapPost("/departments/pdf/download", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateDepartmentsPdfAsync(filter, context);
            var fileName = $"Reporte_Departamentos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            //context.Response.DisableCache();
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            return Results.File(pdfBytes, "application/pdf", fileName);
        })
        .WithName("DownloadDepartmentsPdf")
        .WithSummary("Descarga directa del reporte de departamentos en PDF")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapPost("/departments/excel", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var excelBytes = await reportService.GenerateDepartmentsExcelAsync(filter, context);
            var fileName = $"Reporte_Departamentos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            //context.Response.DisableCache();
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            return Results.File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        })
        .WithName("DownloadDepartmentsExcel")
        .WithSummary("Descarga del reporte de departamentos en Excel")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // ==================== AttendanceSumary ====================

        group.MapPost("/attendancesumary/preview", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateAttendancesumaryPdfAsync(filter, context);

            if (pdfBytes is null || pdfBytes.Length == 0)
            {
                var errorResponse = new PreviewResponseDto(
                    Status: "error",
                    Data: null,
                    Error: new PreviewErrorDto("No se pudo generar el reporte de attendancesumary.")
                );
                //context.Response.DisableCache();
                return Results.Ok(errorResponse);
            }

            var base64 = Convert.ToBase64String(pdfBytes);

            var data = new PdfPreviewDataDto(
                Base64Data: base64,
                FileName: "Reporte_Attendancesumary_Preview.pdf",
                MimeType: "application/pdf"
            );

            var response = new PreviewResponseDto(
                Status: "success",
                Data: data,
                Error: null
            );

            //context.Response.DisableCache();
            return Results.Ok(response);
        })
        .WithName("PreviewAttendancesumarysPdf")
        .WithSummary("Vista previa del reporte de attendancesumary en PDF")
        .Produces<PreviewResponseDto>(StatusCodes.Status200OK, "application/json");

        group.MapPost("/attendancesumary/pdf/download", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var pdfBytes = await reportService.GenerateAttendancesumaryPdfAsync(filter, context);
            var fileName = $"Reporte_Attendancesumary_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            //context.Response.DisableCache();
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            return Results.File(pdfBytes, "application/pdf", fileName);
        })
        .WithName("DownloadAttendancesumaryPdf")
        .WithSummary("Descarga directa del reporte de attendancesumary en PDF")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/pdf");

        group.MapPost("/attendancesumary/excel", async (
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportService reportService,
            HttpContext context) =>
        {
            var excelBytes = await reportService.GenerateAttendancesumaryExcelAsync(filter, context);
            var fileName = $"Reporte_Attendancesumary_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            //context.Response.DisableCache();
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            return Results.File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        })
        .WithName("DownloadAttendancesumaryExcel")
        .WithSummary("Descarga del reporte de attendancesumary en Excel")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // ==================== AUDITORÍA ====================

        group.MapGet("/audits", async (
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? reportType,
            [FromQuery] Guid? userId,
            [FromQuery] int top,
            [FromServices] IReportAuditService auditService,
            HttpContext context) =>
        {
            var audits = await auditService.GetAuditsAsync(startDate, endDate, reportType, userId, top);
            //context.Response.DisableCache();
            return Results.Ok(audits);
        })
        .WithName("GetReportAudits")
        .WithSummary("Obtiene el historial de auditoría de reportes generados")
        .Produces<IEnumerable<ReportAuditDto>>(StatusCodes.Status200OK, "application/json");
    }
}

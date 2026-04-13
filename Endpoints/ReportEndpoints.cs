using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Reports;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;
using WsUtaSystem.Reports.Helpers;

namespace WsUtaSystem.Endpoints;

/// <summary>
/// Endpoints de reportes
/// </summary>
public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
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

        // ==================== V2 GENÉRICOS ====================
        // Reemplazan los 12 endpoints v1 específicos.
        // Para agregar un nuevo reporte: solo registrar un nuevo IReportSource en DI.
        // Rutas: POST /api/v1/rh/reports/v2/{reportType}/preview|pdf/download|excel

        group.MapPost("/v2/{reportType}/preview", async (
            [FromRoute] string reportType,
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportServiceV2 reportServiceV2,
            HttpContext context) =>
        {
            if (!ReportTypeMapper.TryParse(reportType, out var type))
            {
                var slugs = string.Join(", ", ReportTypeMapper.GetRegisteredSlugs());
                return Results.BadRequest(ReportPreviewResponseBuilder.BuildError(
                    $"Tipo de reporte no reconocido: '{reportType}'. Valores válidos: {slugs}"));
            }

            var pdfBytes = await reportServiceV2.GeneratePdfAsync(type, filter, context);

            if (pdfBytes is null || pdfBytes.Length == 0)
                return Results.Ok(ReportPreviewResponseBuilder.BuildError(
                    $"No se pudo generar la vista previa del reporte '{reportType}'."));

            var tempDefinition = new ReportDefinition
            {
                Title      = reportType,
                FilePrefix = $"Reporte_{reportType}"
            };
            return Results.Ok(ReportPreviewResponseBuilder.BuildSuccess(pdfBytes, tempDefinition));
        })
        .WithName("V2PreviewReport")
        .WithSummary("[v2] Vista previa genérica del reporte en PDF")
        .Produces<PreviewResponseDto>(StatusCodes.Status200OK, "application/json")
        .Produces<PreviewResponseDto>(StatusCodes.Status400BadRequest, "application/json");

        group.MapPost("/v2/{reportType}/pdf/download", async (
            [FromRoute] string reportType,
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportServiceV2 reportServiceV2,
            HttpContext context) =>
        {
            if (!ReportTypeMapper.TryParse(reportType, out var type))
            {
                var slugs = string.Join(", ", ReportTypeMapper.GetRegisteredSlugs());
                return Results.BadRequest(new { error = $"Tipo de reporte no reconocido: '{reportType}'. Valores válidos: {slugs}" });
            }

            var pdfBytes = await reportServiceV2.GeneratePdfAsync(type, filter, context);
            var fileName = ReportFileNameBuilder.Build($"Reporte_{reportType}", ReportFormat.Pdf);
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return Results.File(pdfBytes, ReportFileNameBuilder.GetMimeType(ReportFormat.Pdf), fileName);
        })
        .WithName("V2DownloadReportPdf")
        .WithSummary("[v2] Descarga genérica del reporte en PDF")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/pdf")
        .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/v2/{reportType}/excel", async (
            [FromRoute] string reportType,
            [FromBody] ReportFilterDto filter,
            [FromServices] IReportServiceV2 reportServiceV2,
            HttpContext context) =>
        {
            if (!ReportTypeMapper.TryParse(reportType, out var type))
            {
                var slugs = string.Join(", ", ReportTypeMapper.GetRegisteredSlugs());
                return Results.BadRequest(new { error = $"Tipo de reporte no reconocido: '{reportType}'. Valores válidos: {slugs}" });
            }

            var excelBytes = await reportServiceV2.GenerateExcelAsync(type, filter, context);
            var fileName   = ReportFileNameBuilder.Build($"Reporte_{reportType}", ReportFormat.Excel);
            context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            return Results.File(excelBytes, ReportFileNameBuilder.GetMimeType(ReportFormat.Excel), fileName);
        })
        .WithName("V2DownloadReportExcel")
        .WithSummary("[v2] Descarga genérica del reporte en Excel")
        .Produces<FileResult>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        .Produces(StatusCodes.Status400BadRequest);
    }
}

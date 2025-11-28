using System.Diagnostics;
using System.Text.Json;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Reports;
using WsUtaSystem.Application.Services.Reports.Configuration;
using WsUtaSystem.Application.Services.Reports.Generators;
using WsUtaSystem.Middleware;

namespace WsUtaSystem.Application.Services.Reports;

/// <summary>
/// Servicio principal de generación de reportes
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _repository;
    private readonly IReportAuditService _auditService;
    private readonly ReportConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository repository,
        IReportAuditService auditService,
        ReportConfiguration config,
        IWebHostEnvironment env,
        ILogger<ReportService> logger)
    {
        _repository = repository;
        _auditService = auditService;
        _config = config;
        _env = env;
        _logger = logger;
    }

    #region Empleados

    public async Task<byte[]> GenerateEmployeesPdfAsync(ReportFilterDto filter, HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var userEmail = context.User.Identity?.Name ?? "anonymous";
        
        try
        {
            var data = await _repository.GetEmployeesReportDataAsync(filter);
            var generator = new EmployeeReportGenerator(_config, _env);
            var pdfBytes = generator.GeneratePdf(data, filter, userEmail);

            stopwatch.Stop();

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Employees", "PDF", filter, pdfBytes.Length, 
                    (int)stopwatch.ElapsedMilliseconds, context, true, null, "Reporte_Empleados.pdf");
            }

            _logger.LogInformation("Generated Employees PDF report in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return pdfBytes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating Employees PDF report");
            
            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Employees", "PDF", filter, null, 
                    (int)stopwatch.ElapsedMilliseconds, context, false, ex.Message, null);
            }
            
            throw;
        }
    }

    public async Task<byte[]> GenerateEmployeesExcelAsync(ReportFilterDto filter, HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var userEmail = context.User.Identity?.Name ?? "anonymous";
        
        try
        {
            var data = await _repository.GetEmployeesReportDataAsync(filter);
            var generator = new EmployeeReportGenerator(_config, _env);
            var excelBytes = generator.GenerateExcel(data, userEmail);

            stopwatch.Stop();

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Employees", "Excel", filter, excelBytes.Length, 
                    (int)stopwatch.ElapsedMilliseconds, context, true, null, "Reporte_Empleados.xlsx");
            }

            _logger.LogInformation("Generated Employees Excel report in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return excelBytes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating Employees Excel report");
            
            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Employees", "Excel", filter, null, 
                    (int)stopwatch.ElapsedMilliseconds, context, false, ex.Message, null);
            }
            
            throw;
        }
    }

    #endregion

    #region Asistencia

    public async Task<byte[]> GenerateAttendancePdfAsync(ReportFilterDto filter, HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var userEmail = context.User.Identity?.Name ?? "anonymous";
        
        try
        {
            var data = await _repository.GetAttendanceReportDataAsync(filter);
            var generator = new AttendanceReportGenerator(_config, _env);
            var pdfBytes = generator.GeneratePdf(data, filter, userEmail);

            stopwatch.Stop();

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Attendance", "PDF", filter, pdfBytes.Length, 
                    (int)stopwatch.ElapsedMilliseconds, context, true, null, "Reporte_Asistencia.pdf");
            }

            _logger.LogInformation("Generated Attendance PDF report in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return pdfBytes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating Attendance PDF report");
            
            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Attendance", "PDF", filter, null, 
                    (int)stopwatch.ElapsedMilliseconds, context, false, ex.Message, null);
            }
            
            throw;
        }
    }

    public async Task<byte[]> GenerateAttendanceExcelAsync(ReportFilterDto filter, HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var userEmail = context.User.Identity?.Name ?? "anonymous";
        
        try
        {
            var data = await _repository.GetAttendanceReportDataAsync(filter);
            var generator = new AttendanceReportGenerator(_config, _env);
            var excelBytes = generator.GenerateExcel(data, userEmail);

            stopwatch.Stop();

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Attendance", "Excel", filter, excelBytes.Length, 
                    (int)stopwatch.ElapsedMilliseconds, context, true, null, "Reporte_Asistencia.xlsx");
            }

            _logger.LogInformation("Generated Attendance Excel report in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return excelBytes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating Attendance Excel report");
            
            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Attendance", "Excel", filter, null, 
                    (int)stopwatch.ElapsedMilliseconds, context, false, ex.Message, null);
            }
            
            throw;
        }
    }

    #endregion

    #region Departamentos

    public async Task<byte[]> GenerateDepartmentsPdfAsync(ReportFilterDto filter, HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var userEmail = context.User.Identity?.Name ?? "anonymous";
        
        try
        {
            var data = await _repository.GetDepartmentsReportDataAsync(filter);
            var generator = new DepartmentReportGenerator(_config, _env);
            var pdfBytes = generator.GeneratePdf(data, filter, userEmail);

            stopwatch.Stop();

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Departments", "PDF", filter, pdfBytes.Length, 
                    (int)stopwatch.ElapsedMilliseconds, context, true, null, "Reporte_Departamentos.pdf");
            }

            _logger.LogInformation("Generated Departments PDF report in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return pdfBytes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating Departments PDF report");
            
            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Departments", "PDF", filter, null, 
                    (int)stopwatch.ElapsedMilliseconds, context, false, ex.Message, null);
            }
            
            throw;
        }
    }

    public async Task<byte[]> GenerateDepartmentsExcelAsync(ReportFilterDto filter, HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var userEmail = context.User.Identity?.Name ?? "anonymous";
        
        try
        {
            var data = await _repository.GetDepartmentsReportDataAsync(filter);
            var generator = new DepartmentReportGenerator(_config, _env);
            var excelBytes = generator.GenerateExcel(data, userEmail);

            stopwatch.Stop();

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Departments", "Excel", filter, excelBytes.Length, 
                    (int)stopwatch.ElapsedMilliseconds, context, true, null, "Reporte_Departamentos.xlsx");
            }

            _logger.LogInformation("Generated Departments Excel report in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return excelBytes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating Departments Excel report");
            
            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Departments", "Excel", filter, null, 
                    (int)stopwatch.ElapsedMilliseconds, context, false, ex.Message, null);
            }
            
            throw;
        }
    }

    #endregion

    #region Private Helpers

    private async Task CreateAuditAsync(
        string reportType,
        string reportFormat,
        ReportFilterDto filter,
        long? fileSizeBytes,
        int generationTimeMs,
        HttpContext context,
        bool success,
        string? errorMessage,
        string? fileName)
    {
        try
        {
            var userIdStr = context.GetUserId();
            var userId = Guid.TryParse(userIdStr, out var uid) ? uid : (Guid?)null;
            var userEmail = context.User.Identity?.Name ?? "anonymous";
            var clientIp = context.Connection.RemoteIpAddress?.ToString();

            var audit = new CreateReportAuditDto
            {
                UserId = userId,
                UserEmail = userEmail,
                ReportType = reportType,
                ReportFormat = reportFormat,
                FiltersApplied = JsonSerializer.Serialize(filter),
                FileSizeBytes = fileSizeBytes,
                GenerationTimeMs = generationTimeMs,
                ClientIp = clientIp,
                Success = success,
                ErrorMessage = errorMessage,
                FileName = fileName
            };

            await _auditService.CreateAuditAsync(audit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit record");
            // No lanzar excepción para no interrumpir el flujo principal
        }
    }

    #region Attendancesumary
    public async Task<byte[]> GenerateAttendancesumaryPdfAsync(ReportFilterDto filter, HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var userEmail = context.User.Identity?.Name ?? "anonymous";

        try
        {
            var data = await _repository.GetAttendanceSumaryReportDataAsync(filter);
            var generator = new AttendanceSumaryReportGenerator(_config, _env);
            var pdfBytes = generator.GeneratePdf(data, filter, userEmail);

            stopwatch.Stop();

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Employees", "PDF", filter, pdfBytes.Length,
                    (int)stopwatch.ElapsedMilliseconds, context, true, null, "Reporte_Empleados.pdf");
            }

            _logger.LogInformation("Generated Employees PDF report in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating Employees PDF report");

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Employees", "PDF", filter, null,
                    (int)stopwatch.ElapsedMilliseconds, context, false, ex.Message, null);
            }

            throw;
        }
    }
    #endregion
    public async Task<byte[]> GenerateAttendancesumaryExcelAsync(ReportFilterDto filter, HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var userEmail = context.User.Identity?.Name ?? "anonymous";

        try
        {
            var data = await _repository.GetAttendanceSumaryReportDataAsync(filter);
            var generator = new AttendanceSumaryReportGenerator(_config, _env);
            var excelBytes = generator.GenerateExcel(data, userEmail);

            stopwatch.Stop();

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Employees", "Excel", filter, excelBytes.Length,
                    (int)stopwatch.ElapsedMilliseconds, context, true, null, "Reporte_Empleados.xlsx");
            }

            _logger.LogInformation("Generated Employees Excel report in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return excelBytes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating Employees Excel report");

            if (_config.EnableAudit)
            {
                await CreateAuditAsync("Employees", "Excel", filter, null,
                    (int)stopwatch.ElapsedMilliseconds, context, false, ex.Message, null);
            }

            throw;
        }
    }

    #endregion Attendancesumary
}

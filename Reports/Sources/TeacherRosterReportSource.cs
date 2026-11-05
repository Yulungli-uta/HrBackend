using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.DTOs.TeacherStructure;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Reporte de personal docente. Por defecto, docentes titulares: una fila por docente con estructura docente vigente,
/// con categoría, nivel, dedicación, horas, dependencia y número de documento de la acción de personal (la de
/// nombramiento vigente). Con <c>TeacherKind = NO_TITULAR</c>: docentes con contrato ocasional (dedicación, horas,
/// tipo y número de contrato, vigencia).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Usa <see cref="ITeacherRosterService"/> / <see cref="INonTitularRosterService"/>: la MISMA consulta que la página "Personal Docente Titular",
/// así que cifras de pantalla y reporte coinciden siempre.</item>
/// <item>Las columnas de sueldo solo se incluyen si el usuario tiene <c>SALARY_HISTORY.READ</c> (verificado
/// en el servidor contra los roles del token; sin permiso la columna no existe en PDF ni Excel).</item>
/// <item><see cref="ReportFilterDto.IsActive"/>: true = activos, false = inactivos, null = todos. En no titulares
/// equivale a contrato vigente / vencido / todos.</item>
/// </list>
/// </remarks>
public sealed class TeacherRosterReportSource : IReportSource
{
    private const string SalaryPermission = "SALARY_HISTORY.READ";

    private readonly ITeacherRosterService _service;
    private readonly INonTitularRosterService _nonTitularService;
    private readonly IUserActionPermissionService _permissions;
    private readonly ILogger<TeacherRosterReportSource> _logger;

    public ReportType ReportType => ReportType.TeacherRoster;

    private const string ColNro         = "nro";
    private const string ColIdCard      = "id_card";
    private const string ColFullName    = "full_name";
    private const string ColDepartment  = "department";
    private const string ColCategory    = "category";
    private const string ColLevel       = "level";
    private const string ColDedication  = "dedication";
    private const string ColHours       = "weekly_hours";
    private const string ColDocument    = "document";
    private const string ColStatus      = "status";
    private const string ColRmu         = "rmu";

    private static readonly IReadOnlyList<ReportColumn> _baseColumns =
    [
        new(ColNro,         "Nro",          Width: 0.5f, Alignment: ColumnAlignment.Center),
        new(ColIdCard,      "Cédula",       Width: 1.5f),
        new(ColFullName,    "Docente",      Width: 2.5f),
        new(ColDepartment,  "Dependencia",  Width: 2.1f),
        new(ColCategory,    "Categoría",    Width: 1.4f),
        new(ColLevel,       "Nivel",        Width: 0.8f, Alignment: ColumnAlignment.Center),
        new(ColDedication,  "Dedicación",   Width: 1.3f),
        new(ColHours,       "Horas/sem.",   Width: 0.8f, Alignment: ColumnAlignment.Right),
        new(ColDocument,    "N° Documento", Width: 2.0f),
        new(ColStatus,      "Estado",       Width: 0.8f, Alignment: ColumnAlignment.Center),
    ];

    private const string ColContractType = "contract_type";
    private const string ColContractCode  = "contract_code";
    private const string ColStart         = "contract_start";
    private const string ColEnd           = "contract_end";

    private static readonly IReadOnlyList<ReportColumn> _nonTitularColumns =
    [
        new(ColNro,          "Nro",           Width: 0.5f, Alignment: ColumnAlignment.Center),
        new(ColIdCard,       "Cédula",        Width: 1.5f),
        new(ColFullName,     "Docente",       Width: 2.6f),
        new(ColDepartment,   "Dependencia",   Width: 2.3f),
        new(ColDedication,   "Dedicación",    Width: 1.4f),
        new(ColHours,        "Horas/sem.",    Width: 0.8f, Alignment: ColumnAlignment.Right),
        new(ColContractType, "Tipo contrato", Width: 2.0f),
        new(ColContractCode, "N° Contrato",   Width: 1.7f),
        new(ColStart,        "Inicio",        Width: 1.0f, Alignment: ColumnAlignment.Center),
        new(ColEnd,          "Fin",           Width: 1.0f, Alignment: ColumnAlignment.Center),
        new(ColStatus,       "Estado",        Width: 0.9f, Alignment: ColumnAlignment.Center),
    ];

    private static readonly IReadOnlyList<ReportColumn> _salaryColumns =
    [
        new(ColRmu,      "RMU",           Width: 1.0f, Alignment: ColumnAlignment.Right),
    ];

    public TeacherRosterReportSource(
        ITeacherRosterService service,
        INonTitularRosterService nonTitularService,
        IUserActionPermissionService permissions,
        ILogger<TeacherRosterReportSource> logger)
    {
        _service     = service     ?? throw new ArgumentNullException(nameof(service));
        _nonTitularService = nonTitularService ?? throw new ArgumentNullException(nameof(nonTitularService));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var canViewSalary = await _permissions.HasPermissionAsync(roles, SalaryPermission, context.RequestAborted);

        if (string.Equals(filter.TeacherKind, "NO_TITULAR", StringComparison.OrdinalIgnoreCase))
            return await BuildNonTitularAsync(filter, context, canViewSalary);

        var rosterFilter = new TeacherRosterFilterDto(
            CategoryTypeId: filter.CategoryTypeId,
            LevelTypeId: filter.LevelTypeId,
            DedicationTypeId: filter.DedicationTypeId,
            DepartmentId: filter.DepartmentId,
            Status: filter.IsActive switch { true => "ACTIVE", false => "INACTIVE", _ => "ALL" });

        _logger.LogInformation(
            "Building TeacherRoster report. Category={Cat}, Level={Lvl}, Dedication={Ded}, DepartmentId={Dept}, Status={Status}, Salary={Salary}",
            filter.CategoryTypeId, filter.LevelTypeId, filter.DedicationTypeId, filter.DepartmentId,
            rosterFilter.Status, canViewSalary);

        var records = await _service.GetAllAsync(rosterFilter, canViewSalary, context.RequestAborted);

        return new ReportDefinition
        {
            Title       = "Docentes Titulares",
            FilePrefix  = "Reporte_Docentes_Titulares",
            Subtitle    = BuildSubtitle(rosterFilter.Status, records),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = canViewSalary ? [.. _baseColumns, .. _salaryColumns] : _baseColumns,
            Rows        = BuildRows(records, canViewSalary),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true,
        };
    }

    private async Task<ReportDefinition> BuildNonTitularAsync(ReportFilterDto filter, HttpContext context, bool canViewSalary)
    {
        var nonTitularFilter = new NonTitularRosterFilterDto(
            DedicationTypeId: filter.DedicationTypeId,
            DepartmentId: filter.DepartmentId,
            Status: filter.IsActive switch { true => "ACTIVE", false => "EXPIRED", _ => "ALL" });

        _logger.LogInformation(
            "Building TeacherRoster (NO_TITULAR) report. Dedication={Ded}, DepartmentId={Dept}, Status={Status}, Salary={Salary}",
            filter.DedicationTypeId, filter.DepartmentId, nonTitularFilter.Status, canViewSalary);

        var records = await _nonTitularService.GetAllAsync(nonTitularFilter, canViewSalary, context.RequestAborted);

        var statusText = nonTitularFilter.Status == "ACTIVE" ? "Contrato vigente" : nonTitularFilter.Status == "EXPIRED" ? "Contrato vencido" : "Todos";
        var byDedication = records
            .GroupBy(r => r.DedicationName is null ? "Sin modalidad" : AcademicLabels.Dedication(r.DedicationName))
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key}: {g.Count()}");

        return new ReportDefinition
        {
            Title       = "Docentes No Titulares",
            FilePrefix  = "Reporte_Docentes_No_Titulares",
            Subtitle    = string.Join(" | ", new[] { $"Estado: {statusText}", $"Total docentes: {records.Count}", string.Join(" · ", byDedication) }
                .Where(p => !string.IsNullOrWhiteSpace(p))),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns     = canViewSalary ? [.. _nonTitularColumns, .. _salaryColumns] : _nonTitularColumns,
            Rows        = BuildNonTitularRows(records, canViewSalary),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Landscape,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true,
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildNonTitularRows(
        IReadOnlyList<NonTitularRosterItemDto> records, bool canViewSalary)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        var nro = 1;
        foreach (var r in records)
        {
            var row = new Dictionary<string, object?>
            {
                [ColNro]          = nro++,
                [ColIdCard]       = r.IdCard ?? "-",
                [ColFullName]     = r.FullName,
                [ColDepartment]   = Department(r.FacultyName, r.DepartmentName),
                [ColDedication]   = r.DedicationName is null ? "-" : AcademicLabels.Dedication(r.DedicationName),
                [ColHours]        = r.ContractedHours?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-",
                [ColContractType] = r.ContractTypeName ?? "-",
                [ColContractCode] = string.IsNullOrWhiteSpace(r.ContractCode) ? "-" : r.ContractCode,
                [ColStart]        = r.ContractStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                [ColEnd]          = r.ContractEndDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-",
                [ColStatus]       = r.ContractState == "VIGENTE" ? "Vigente" : "Vencido",
            };

            if (canViewSalary)
            {
                row[ColRmu] = r.Rmu?.ToString("N2", CultureInfo.InvariantCulture) ?? "-";
            }

            rows.Add(row);
        }
        return rows;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<TeacherRosterItemDto> records, bool canViewSalary)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        var nro = 1;
        foreach (var r in records)
        {
            var row = new Dictionary<string, object?>
            {
                [ColNro]         = nro++,
                [ColIdCard]      = r.IdCard ?? "-",
                [ColFullName]    = r.FullName,
                [ColDepartment]  = Department(r),
                [ColCategory]    = AcademicLabels.Category(r.CategoryName),
                [ColLevel]       = AcademicLabels.Level(r.LevelName),
                [ColDedication]  = AcademicLabels.Dedication(r.DedicationName),
                [ColHours]       = r.WeeklyClassHours?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-",
                [ColDocument]    = string.IsNullOrWhiteSpace(r.AppointmentActionNumber) ? "-" : r.AppointmentActionNumber,
                [ColStatus]      = r.EmployeeIsActive ? "Activo" : "Inactivo",
            };

            if (canViewSalary)
            {
                row[ColRmu] = r.Rmu?.ToString("N2", CultureInfo.InvariantCulture) ?? "-";
            }

            rows.Add(row);
        }
        return rows;
    }

    /// <summary>Facultad y, si difiere, la dependencia específica (carrera, dirección…).</summary>
    private static string Department(TeacherRosterItemDto r) => Department(r.FacultyName, r.DepartmentName);

    private static string Department(string? facultyName, string? departmentName)
    {
        if (string.IsNullOrWhiteSpace(facultyName)) return departmentName ?? "-";
        return string.IsNullOrWhiteSpace(departmentName) || departmentName == facultyName
            ? facultyName
            : $"{facultyName} / {departmentName}";
    }

    private static string BuildSubtitle(string status, IReadOnlyList<TeacherRosterItemDto> records)
    {
        var parts = new List<string>
        {
            $"Estado: {(status == "ACTIVE" ? "Activos" : status == "INACTIVE" ? "Inactivos" : "Todos")}",
            $"Total docentes: {records.Count}",
        };

        var byCategory = records
            .GroupBy(r => AcademicLabels.Category(r.CategoryName))
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key}: {g.Count()}");
        parts.Add(string.Join(" · ", byCategory));

        var byDedication = records
            .GroupBy(r => AcademicLabels.Dedication(r.DedicationName))
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Key}: {g.Count()}");
        parts.Add(string.Join(" · ", byDedication));

        return string.Join(" | ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}

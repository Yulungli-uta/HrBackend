using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Sources;

/// <summary>
/// Origen de datos para el reporte de subsidio de cargas familiares. Cuenta, por
/// empleado, las cargas familiares con <c>StatusTypeId = APROBADO</c> que califican:
/// menores de la edad tope parametrizada (<c>FAMILY_SUBSIDY_MAX_AGE</c>, año cumplido —
/// se compara la fecha de nacimiento contra la fecha de corte, no una resta de años) o,
/// sin importar la edad, si tienen discapacidad registrada (<c>DisabilityTypeId != null</c>,
/// subsidio permanente). Multiplica la cantidad por el valor base parametrizado
/// (<c>FAMILY_SUBSIDY_BASE_VALUE</c>).
/// </summary>
public sealed class FamilySubsidyReportSource : IReportSource
{
    private const string StatusCategory = "FAMILY_BURDEN_STATUS";
    private const string ApprovedStatusName = "APROBADO";
    private const string MaxAgeParam = "FAMILY_SUBSIDY_MAX_AGE";
    private const decimal MaxAgeDefault = 18m;

    // Fórmula real (Cláusula Vigésima Séptima, Décimo Octavo Contrato Colectivo UTA):
    // 1% del SBU por cada carga familiar calificada — NO un valor fijo institucional.
    // FAMILY_SUBSIDY_BASE_VALUE quedó obsoleto 2026-08-17 (no se borra, solo se desactiva).
    private const string SbuParam = "SBU_VALUE";
    private const decimal SbuDefault = 0m;
    private const string SubsidyPercentParam = "FAMILY_SUBSIDY_PERCENT";
    private const decimal SubsidyPercentDefault = 0m;

    private readonly AppDbContext _db;
    private readonly IParametersRepository _parametersRepository;
    private readonly ILogger<FamilySubsidyReportSource> _logger;

    public ReportType ReportType => ReportType.FamilySubsidySummary;

    private const string ColNro = "nro";
    private const string ColIdCard = "id_card";
    private const string ColFullName = "full_name";
    private const string ColDepartment = "department_name";
    private const string ColDependents = "qualifying_dependents";
    private const string ColUnitValue = "unit_value";
    private const string ColTotalValue = "total_value";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColNro,         "Nro",                    Width: 0.6f, Alignment: ColumnAlignment.Center),
        new(ColIdCard,      "Cédula",                 Width: 1.4f),
        new(ColFullName,    "Nombres y Apellidos",    Width: 2.6f),
        new(ColDepartment,  "Dependencia",            Width: 1.8f),
        new(ColDependents,  "Cargas que Califican",   Width: 1.2f, Alignment: ColumnAlignment.Center),
        new(ColUnitValue,   "Valor",                  Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColTotalValue,  "Total",                  Width: 1.0f, Alignment: ColumnAlignment.Right),
    ];

    public FamilySubsidyReportSource(
        AppDbContext db,
        IParametersRepository parametersRepository,
        ILogger<FamilySubsidyReportSource> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _parametersRepository = parametersRepository ?? throw new ArgumentNullException(nameof(parametersRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ReportDefinition> BuildAsync(ReportFilterDto filter, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.RequestAborted;

        _logger.LogInformation(
            "Building FamilySubsidySummary report. DepartmentId={DeptId}, EmployeeId={EmpId}",
            filter.DepartmentId, filter.EmployeeId);

        var maxAge = await GetParameterDecimalAsync(MaxAgeParam, MaxAgeDefault, ct);
        var sbu = await GetParameterDecimalAsync(SbuParam, SbuDefault, ct);
        var subsidyPercent = await GetParameterDecimalAsync(SubsidyPercentParam, SubsidyPercentDefault, ct);
        var unitValue = Math.Round(sbu * subsidyPercent / 100m, 2);
        var cutoffDate = DateOnly.FromDateTime(DateTime.Today).AddYears(-(int)maxAge);

        var approvedStatusId = await _db.Set<RefTypes>()
            .Where(r => r.Category == StatusCategory && r.Name == ApprovedStatusName)
            .Select(r => (int?)r.TypeId)
            .FirstOrDefaultAsync(ct);

        var query =
            from fb in _db.Set<FamilyBurden>().AsNoTracking()
            where fb.StatusTypeId == approvedStatusId
               && (fb.BirthDate > cutoffDate || fb.DisabilityTypeId != null)
            join emp in _db.Employees.AsNoTracking() on fb.PersonId equals emp.PersonID
            join person in _db.People.AsNoTracking() on fb.PersonId equals person.PersonId
            join dept in _db.Departments.AsNoTracking() on emp.DepartmentId equals dept.DepartmentId into deptJoin
            from dept in deptJoin.DefaultIfEmpty()
            select new { fb, emp, person, dept };

        // Por defecto solo empleados activos (mismo criterio que los reportes SIIES):
        // el subsidio es un beneficio de nómina vigente, no debe sumar a alguien que ya
        // no trabaja en la institución salvo que se pida explícitamente para revisión histórica.
        if (filter.IncludeInactive != true)
            query = query.Where(x => x.emp.IsActive);

        if (filter.EmployeeId.HasValue)
            query = query.Where(x => x.emp.EmployeeId == filter.EmployeeId.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(x => x.emp.DepartmentId == filter.DepartmentId.Value);

        // Rango de fechas: sobre la fecha de APROBACIÓN de la carga (cuándo empezó a
        // contar para el subsidio), no sobre BirthDate ni CreatedAt. Opcional — sin
        // filtro, muestra todas las cargas aprobadas vigentes hoy.
        if (filter.StartDate.HasValue)
            query = query.Where(x => x.fb.ApprovedAt != null && x.fb.ApprovedAt.Value >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(x => x.fb.ApprovedAt != null && x.fb.ApprovedAt.Value <= filter.EndDate.Value);

        // Régimen laboral: prioriza EmployeeLaborRegime; si el empleado no tiene ningún
        // registro activo ahí, cae a Employees.EmployeeType legacy (mismo criterio que
        // el resto de reportes de esta sesión, en vez de excluirlo en silencio).
        if (filter.LaborRegimeId.HasValue)
        {
            var regimeId = filter.LaborRegimeId.Value;
            query = query.Where(x =>
                _db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.emp.EmployeeId && r.IsActive && r.LaborRegimeId == regimeId)
                || (!_db.Set<EmployeeLaborRegime>().Any(r => r.EmployeeId == x.emp.EmployeeId && r.IsActive)
                    && x.emp.EmployeeType == regimeId));
        }

        var grouped = await query
            .GroupBy(x => new
            {
                x.emp.EmployeeId,
                x.person.IdCard,
                FullName = x.person.LastName + " " + x.person.FirstName,
                DepartmentName = x.dept != null ? x.dept.Name : null
            })
            .Select(g => new FamilySubsidyReportDto
            {
                EmployeeId = g.Key.EmployeeId,
                IdCard = g.Key.IdCard,
                FullName = g.Key.FullName,
                DepartmentName = g.Key.DepartmentName,
                QualifyingDependents = g.Count()
            })
            .OrderBy(r => r.FullName)
            .ToListAsync(ct);

        var records = grouped
            .Select(r => r with { UnitValue = unitValue, TotalValue = r.QualifyingDependents * unitValue })
            .ToList();

        _logger.LogInformation("FamilySubsidySummary report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title = "Subsidio por Cargas Familiares",
            FilePrefix = "Reporte_Subsidio_Cargas_Familiares",
            Subtitle = BuildSubtitle(records, unitValue, maxAge, filter),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns = _columns,
            Rows = BuildRows(records),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Portrait,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<FamilySubsidyReportDto> records)
    {
        var rows = new List<IReadOnlyDictionary<string, object?>>(records.Count);
        var nro = 1;
        foreach (var r in records)
        {
            rows.Add(new Dictionary<string, object?>
            {
                [ColNro] = nro++,
                [ColIdCard] = r.IdCard,
                [ColFullName] = r.FullName,
                [ColDepartment] = r.DepartmentName ?? "—",
                [ColDependents] = r.QualifyingDependents,
                [ColUnitValue] = r.UnitValue.ToString("N2", CultureInfo.InvariantCulture),
                [ColTotalValue] = r.TotalValue.ToString("N2", CultureInfo.InvariantCulture),
            });
        }
        return rows;
    }

    private static string BuildSubtitle(
        IReadOnlyList<FamilySubsidyReportDto> records, decimal unitValue, decimal maxAge, ReportFilterDto filter)
    {
        var parts = new List<string>();

        if (filter.StartDate.HasValue || filter.EndDate.HasValue)
        {
            var from = filter.StartDate?.ToString("dd/MM/yyyy") ?? "…";
            var to = filter.EndDate?.ToString("dd/MM/yyyy") ?? "…";
            parts.Add($"Aprobadas entre: {from} - {to}");
        }

        parts.Add(filter.IncludeInactive == true ? "Incluye empleados inactivos" : "Solo empleados activos");
        parts.Add($"Edad tope: {maxAge:N0} años (excepto discapacidad, permanente)");
        parts.Add($"Valor por carga: {unitValue.ToString("N2", CultureInfo.InvariantCulture)}");
        parts.Add($"Total empleados: {records.Count}");
        parts.Add($"Total general: {records.Sum(r => r.TotalValue).ToString("N2", CultureInfo.InvariantCulture)}");

        return string.Join(" | ", parts);
    }

    private async Task<decimal> GetParameterDecimalAsync(string name, decimal defaultValue, CancellationToken ct)
    {
        var list = await _parametersRepository.GetByNameAsync(name, ct);
        var value = list?.FirstOrDefault(p => p.IsActive)?.Pvalues;
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }
}

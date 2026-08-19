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
/// Origen de datos para el reporte de subsidio de antigüedad (Cláusula Vigésima Octava,
/// Décimo Octavo Contrato Colectivo UTA): 0.25% del RMU por cada año completo de
/// antigüedad. RMU sale de <see cref="SalaryHistory"/> (fila más reciente por empleado,
/// libro único desde la unificación 2026-08-14). Los años de antigüedad se cuentan desde
/// <c>Employees.SeniorityDate</c> (fecha de contrato indefinido), o desde <c>HireDate</c>
/// como respaldo si el empleado no tiene <c>SeniorityDate</c> cargado — completos,
/// respetando mes y día (no una resta simple de años).
/// </summary>
public sealed class SeniorityBonusReportSource : IReportSource
{
    private const string SeniorityPercentParam = "SENIORITY_SUBSIDY_PERCENT";
    private const decimal SeniorityPercentDefault = 0m;

    private readonly AppDbContext _db;
    private readonly IParametersRepository _parametersRepository;
    private readonly ILogger<SeniorityBonusReportSource> _logger;

    public ReportType ReportType => ReportType.SeniorityBonusSummary;

    private const string ColNro = "nro";
    private const string ColIdCard = "id_card";
    private const string ColFullName = "full_name";
    private const string ColLaborRegime = "labor_regime_name";
    private const string ColRmu = "rmu";
    private const string ColSeniorityYears = "seniority_years";
    private const string ColUnitValue = "unit_value";
    private const string ColTotalValue = "total_value";

    private static readonly IReadOnlyList<ReportColumn> _columns =
    [
        new(ColNro,            "Nro",                  Width: 0.6f, Alignment: ColumnAlignment.Center),
        new(ColIdCard,         "Cédula",                Width: 1.4f),
        new(ColFullName,       "Nombres y Apellidos",   Width: 2.6f),
        new(ColLaborRegime,    "Modalidad / Régimen",   Width: 1.6f),
        new(ColRmu,            "RMU",                   Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColSeniorityYears, "Años Antigüedad",       Width: 1.0f, Alignment: ColumnAlignment.Center),
        new(ColUnitValue,      "Valor Cálculo",         Width: 1.0f, Alignment: ColumnAlignment.Right),
        new(ColTotalValue,     "Total",                 Width: 1.0f, Alignment: ColumnAlignment.Right),
    ];

    public SeniorityBonusReportSource(
        AppDbContext db,
        IParametersRepository parametersRepository,
        ILogger<SeniorityBonusReportSource> logger)
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
            "Building SeniorityBonusSummary report. DepartmentId={DeptId}, EmployeeId={EmpId}",
            filter.DepartmentId, filter.EmployeeId);

        var seniorityPercent = await GetParameterDecimalAsync(SeniorityPercentParam, SeniorityPercentDefault, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var query =
            from e in _db.Employees.AsNoTracking()
            where e.IsActive
            join person in _db.People.AsNoTracking() on e.PersonID equals person.PersonId
            select new { e, person };

        if (filter.IncludeInactive == true)
        {
            query =
                from e in _db.Employees.AsNoTracking()
                join person in _db.People.AsNoTracking() on e.PersonID equals person.PersonId
                select new { e, person };
        }

        if (filter.EmployeeId.HasValue)
            query = query.Where(x => x.e.EmployeeId == filter.EmployeeId.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(x => x.e.DepartmentId == filter.DepartmentId.Value);

        var employees = await query.ToListAsync(ct);

        // Régimen laboral principal por empleado (mismo criterio que FamilySubsidyReportSource:
        // prioriza EmployeeLaborRegime activo, nunca excluye a quien no tenga fila ahí).
        var employeeIds = employees.Select(x => x.e.EmployeeId).ToList();

        var principalRegimes = await _db.Set<EmployeeLaborRegime>().AsNoTracking()
            .Where(r => employeeIds.Contains(r.EmployeeId) && r.IsActive)
            .GroupBy(r => r.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, LaborRegimeId = g.OrderByDescending(r => r.Id).First().LaborRegimeId })
            .ToListAsync(ct);
        var regimeByEmployee = principalRegimes.ToDictionary(x => x.EmployeeId, x => x.LaborRegimeId);

        var regimeIds = principalRegimes.Select(x => x.LaborRegimeId).Distinct().ToList();
        var regimeNames = await _db.Set<RefTypes>().AsNoTracking()
            .Where(r => regimeIds.Contains(r.TypeId))
            .ToDictionaryAsync(r => r.TypeId, r => r.Name, ct);

        // Filtro de régimen laboral (LaborRegimeId), si se especifica.
        if (filter.LaborRegimeId.HasValue)
        {
            var regimeId = filter.LaborRegimeId.Value;
            employees = employees
                .Where(x => regimeByEmployee.TryGetValue(x.e.EmployeeId, out var r) && r == regimeId)
                .ToList();
        }

        // RMU: última fila de SalaryHistory por empleado (libro único desde 2026-08-14).
        var latestSalaries = await _db.Set<SalaryHistory>().AsNoTracking()
            .Where(s => s.EmployeeId != null && employeeIds.Contains(s.EmployeeId.Value))
            .GroupBy(s => s.EmployeeId)
            .Select(g => g.OrderByDescending(s => s.ChangedAt).ThenByDescending(s => s.SalaryHistoryId).First())
            .ToListAsync(ct);
        var rmuByEmployee = latestSalaries.ToDictionary(s => s.EmployeeId!.Value, s => s.NewSalary);

        var records = new List<SeniorityBonusReportDto>();
        foreach (var x in employees)
        {
            if (!rmuByEmployee.TryGetValue(x.e.EmployeeId, out var rmu) || rmu <= 0)
                continue; // sin RMU registrado, no se puede calcular el subsidio

            var seniorityBase = x.e.SeniorityDate ?? x.e.HireDate;
            var years = YearsCompleted(seniorityBase, today);
            if (years <= 0) continue;

            var unitValue = Math.Round(rmu * seniorityPercent / 100m, 2);
            var total = Math.Round(unitValue * years, 2);

            regimeByEmployee.TryGetValue(x.e.EmployeeId, out var regimeId);
            regimeNames.TryGetValue(regimeId, out var regimeName);

            records.Add(new SeniorityBonusReportDto
            {
                EmployeeId = x.e.EmployeeId,
                IdCard = x.person.IdCard,
                FullName = $"{x.person.LastName} {x.person.FirstName}",
                LaborRegimeName = regimeName,
                Rmu = rmu,
                SeniorityYears = years,
                UnitValue = unitValue,
                TotalValue = total,
            });
        }

        records = records.OrderBy(r => r.FullName).ToList();

        _logger.LogInformation("SeniorityBonusSummary report: {Count} records.", records.Count);

        return new ReportDefinition
        {
            Title = "Subsidio por Antigüedad",
            FilePrefix = "Reporte_Subsidio_Antiguedad",
            Subtitle = BuildSubtitle(records, seniorityPercent),
            GeneratedBy = context.User.Identity?.Name ?? "anonymous",
            GeneratedAt = DateTime.Now,
            Columns = _columns,
            Rows = BuildRows(records),
            Orientation = filter.GetPageOrientation() ?? PageOrientation.Portrait,
            VerticalHeaders = filter.VerticalHeaders ?? false,
            RepeatHeaderOnEveryPage = filter.RepeatHeaderOnEveryPage ?? true
        };
    }

    /// <summary>Años completos entre dos fechas, respetando mes y día (no resta simple de años).</summary>
    private static int YearsCompleted(DateOnly from, DateOnly to)
    {
        var years = to.Year - from.Year;
        var anniversaryThisYear = new DateOnly(from.Year + years, from.Month, from.Day);
        if (anniversaryThisYear > to) years--;
        return years;
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, object?>> BuildRows(
        IReadOnlyList<SeniorityBonusReportDto> records)
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
                [ColLaborRegime] = r.LaborRegimeName ?? "—",
                [ColRmu] = r.Rmu.ToString("N2", CultureInfo.InvariantCulture),
                [ColSeniorityYears] = r.SeniorityYears,
                [ColUnitValue] = r.UnitValue.ToString("N2", CultureInfo.InvariantCulture),
                [ColTotalValue] = r.TotalValue.ToString("N2", CultureInfo.InvariantCulture),
            });
        }
        return rows;
    }

    private static string BuildSubtitle(IReadOnlyList<SeniorityBonusReportDto> records, decimal seniorityPercent)
    {
        var parts = new List<string>
        {
            $"Porcentaje de antigüedad: {seniorityPercent.ToString("N2", CultureInfo.InvariantCulture)}% del RMU por año",
            $"Total empleados: {records.Count}",
            $"Total general: {records.Sum(r => r.TotalValue).ToString("N2", CultureInfo.InvariantCulture)}"
        };
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

using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Implementación de <see cref="IAttendanceCalculationsReportService"/>.
/// Orquesta las consultas del repositorio y aplica validaciones de negocio.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: delega toda la lógica de acceso a datos al repositorio
/// y se concentra únicamente en las reglas de negocio (validación de filtros,
/// valores por defecto, logging).
/// </para>
/// <para>
/// Principio OCP: se puede extender con nuevos métodos de reporte sin modificar
/// los existentes.
/// </para>
/// </remarks>
public sealed class AttendanceCalculationsReportService : IAttendanceCalculationsReportService
{
    private const string FoodSubsidyDailyValueParam = "FOOD_SUBSIDY_DAILY_VALUE";
    private const decimal FoodSubsidyDailyValueDefault = 3.50m;

    private readonly IAttendanceCalculationsReportRepository _repository;
    private readonly IParametersRepository _parametersRepository;
    private readonly ILogger<AttendanceCalculationsReportService> _logger;

    public AttendanceCalculationsReportService(
        IAttendanceCalculationsReportRepository repository,
        IParametersRepository parametersRepository,
        ILogger<AttendanceCalculationsReportService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _parametersRepository = parametersRepository ?? throw new ArgumentNullException(nameof(parametersRepository));
        _logger     = logger     ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<LatenessReportDto>> GetLatenessDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando reporte de atrasos. Período: {Start} - {End} | EmployeeId: {EmpId} | DeptId: {DeptId}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.EmployeeId?.ToString()            ?? "Todos",
            filter.DepartmentId?.ToString()          ?? "Todos");

        var data = await _repository.GetLatenessDataAsync(filter, ct);

        _logger.LogInformation(
            "Reporte de atrasos generado. Total registros: {Count}", data.Count);

        return data;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OvertimeReportDto>> GetOvertimeDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando reporte de horas extras. Período: {Start} - {End} | EmployeeId: {EmpId} | DeptId: {DeptId}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.EmployeeId?.ToString()            ?? "Todos",
            filter.DepartmentId?.ToString()          ?? "Todos");

        var data = await _repository.GetOvertimeDataAsync(filter, ct);

        _logger.LogInformation(
            "Reporte de horas extras generado. Total registros: {Count}", data.Count);

        return data;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AttendanceCrossReportDto>> GetAttendanceCrossDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando reporte cruzado de asistencia. Período: {Start} - {End} | EmployeeId: {EmpId} | DeptId: {DeptId}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.EmployeeId?.ToString()            ?? "Todos",
            filter.DepartmentId?.ToString()          ?? "Todos");

        var data = await _repository.GetAttendanceCrossDataAsync(filter, ct);

        _logger.LogInformation(
            "Reporte cruzado de asistencia generado. Total registros: {Count}", data.Count);

        return data;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AttendanceReportDto>> GetAttendanceDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var data = await _repository.GetAttendanceDataAsync(filter, ct);

        _logger.LogInformation("Reporte de asistencia generado. Total registros: {Count}", data.Count);

        return data;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FoodSubsidySummaryReportDto>> GetFoodSubsidySummaryDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando reporte de subsidio de alimentación. Período: {Start} - {End} | DeptId: {DeptId} | EmployeeId: {EmpId} | Cédula: {IdCard} | RegimeId: {RegimeId}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.DepartmentId?.ToString()   ?? "Todas",
            filter.EmployeeId?.ToString()     ?? "Todos",
            filter.Identification             ?? "Todas",
            filter.LaborRegimeId?.ToString()  ?? "Todos");

        var unitValue = await GetParameterDecimalAsync(FoodSubsidyDailyValueParam, FoodSubsidyDailyValueDefault, ct);
        var data = await _repository.GetFoodSubsidySummaryDataAsync(filter, ct);

        var result = data
            .Select(r => r with
            {
                UnitValue  = unitValue,
                TotalValue = r.DaysWorked * unitValue
            })
            .ToList();

        _logger.LogInformation(
            "Reporte de subsidio de alimentación generado. Total registros: {Count} | Valor diario: {UnitValue}",
            result.Count, unitValue);

        return result;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FoodSubsidyByScheduleReportDto>> GetFoodSubsidyByScheduleDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando reporte de subsidio de alimentación por horario. Período: {Start} - {End} | DeptId: {DeptId} | EmployeeId: {EmpId} | Cédula: {IdCard} | RegimeId: {RegimeId}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.DepartmentId?.ToString()   ?? "Todas",
            filter.EmployeeId?.ToString()     ?? "Todos",
            filter.Identification             ?? "Todas",
            filter.LaborRegimeId?.ToString()  ?? "Todos");

        var unitValue = await GetParameterDecimalAsync(FoodSubsidyDailyValueParam, FoodSubsidyDailyValueDefault, ct);
        var data = await _repository.GetFoodSubsidyByScheduleDataAsync(filter, ct);

        var result = data
            .Select(r => r with
            {
                UnitValue  = unitValue,
                TotalValue = r.JourneysCount * unitValue
            })
            .ToList();

        _logger.LogInformation(
            "Reporte de subsidio de alimentación por horario generado. Total filas (empleado+horario): {Count} | Valor diario: {UnitValue}",
            result.Count, unitValue);

        return result;
    }

    private const int PunchDetailMaxDays = 31;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PunchDetailReportDto>> GetPunchDetailDataAsync(ReportFilterDto filter, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ValidatePunchDetailRange(filter);

        var data = await _repository.GetPunchDetailDataAsync(filter, ct);
        _logger.LogInformation("Reporte detallado de picadas generado. Filas: {Count}", data.Count);
        return data;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<PunchDetailReportDto>> GetPunchDetailSummaryAsync(ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ValidatePunchDetailRange(filter);
        return await _repository.GetPunchDetailSummaryAsync(filter, page, pageSize, ct);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<PunchDetailReportDto>> GetLatenessDetailAsync(ReportFilterDto filter, int page, int pageSize, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!filter.EmployeeId.HasValue || filter.EmployeeId.Value <= 0)
            throw new BusinessRuleException("Indique el empleado del que desea ver el detalle de atrasos.");
        if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
            throw new BusinessRuleException("Indique el rango de fechas (desde y hasta).");
        if (filter.EndDate.Value.Date < filter.StartDate.Value.Date)
            throw new BusinessRuleException("La fecha \"hasta\" no puede ser anterior a la fecha \"desde\".");

        return await _repository.GetLatenessDetailAsync(filter, page, pageSize, ct);
    }

    private static void ValidatePunchDetailRange(ReportFilterDto filter)
    {
        if (!filter.StartDate.HasValue || !filter.EndDate.HasValue)
            throw new BusinessRuleException("Indique el rango de fechas (desde y hasta).");

        var start = DateOnly.FromDateTime(filter.StartDate.Value);
        var end = DateOnly.FromDateTime(filter.EndDate.Value);
        if (end < start)
            throw new BusinessRuleException("La fecha \"hasta\" no puede ser anterior a la fecha \"desde\".");
        if (end.DayNumber - start.DayNumber + 1 > PunchDetailMaxDays)
            throw new BusinessRuleException($"El rango máximo del reporte es de {PunchDetailMaxDays} días.");
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AttendanceNoveltyReportDto>> GetAttendanceNoveltiesDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando reporte de novedades de asistencia. Período: {Start} - {End} | DeptId: {DeptId} | EmployeeId: {EmpId} | Cédula: {IdCard} | RegimeId: {RegimeId}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.DepartmentId?.ToString()   ?? "Todas",
            filter.EmployeeId?.ToString()     ?? "Todos",
            filter.Identification             ?? "Todas",
            filter.LaborRegimeId?.ToString()  ?? "Todos");

        var data = await _repository.GetAttendanceNoveltiesDataAsync(filter, ct);

        _logger.LogInformation("Reporte de novedades de asistencia generado. Total novedades: {Count}", data.Count);

        return data;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AttendanceNoveltyReportDto>> GetAttendanceNoveltiesSummaryAsync(
        ReportFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando resumen de novedades de asistencia. Período: {Start} - {End} | DeptId: {DeptId} | RegimeId: {RegimeId} | Búsqueda: {Search} | Tipo: {Type} | Página: {Page}/{PageSize}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.DepartmentId?.ToString()  ?? "Todas",
            filter.LaborRegimeId?.ToString() ?? "Todos",
            filter.SearchText ?? "N/A",
            filter.NoveltyType ?? "Todos",
            page, pageSize);

        var data = await _repository.GetAttendanceNoveltiesSummaryAsync(filter, page, pageSize, ct);

        _logger.LogInformation("Resumen de novedades de asistencia generado. Total: {Count}", data.TotalCount);

        return data;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<LatenessSummaryReportDto>> GetLatenessSummaryDataAsync(
        ReportFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando resumen de atrasos. Período: {Start} - {End} | DeptId: {DeptId} | RegimeId: {RegimeId} | Búsqueda: {Search} | Página: {Page}/{PageSize}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.DepartmentId?.ToString()  ?? "Todas",
            filter.LaborRegimeId?.ToString() ?? "Todos",
            filter.SearchText ?? "N/A",
            page, pageSize);

        var data = await _repository.GetLatenessSummaryDataAsync(filter, page, pageSize, ct);

        _logger.LogInformation("Resumen de atrasos generado. Total empleados: {Count}", data.TotalCount);

        return data;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<NightHoursSummaryReportDto>> GetNightHoursSummaryDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando reporte de horas nocturnas. Período: {Start} - {End} | DeptId: {DeptId} | EmployeeId: {EmpId} | Cédula: {IdCard} | RegimeId: {RegimeId}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.DepartmentId?.ToString()   ?? "Todas",
            filter.EmployeeId?.ToString()     ?? "Todos",
            filter.Identification             ?? "Todas",
            filter.LaborRegimeId?.ToString()  ?? "Todos");

        var data = await _repository.GetNightHoursSummaryDataAsync(filter, ct);

        _logger.LogInformation(
            "Reporte de horas nocturnas generado. Total empleados: {Count}", data.Count);

        return data;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OvertimePlanDailyReportDto>> GetOvertimePlanDailyDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando reporte diario de planificación vs. ejecución de horas extra. Período: {Start} - {End} | DeptId: {DeptId} | EmployeeId: {EmpId}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.DepartmentId?.ToString() ?? "Todas",
            filter.EmployeeId?.ToString()   ?? "Todos");

        var data = await _repository.GetOvertimePlanDailyDataAsync(filter, ct);

        _logger.LogInformation("Reporte diario de planificación vs. ejecución generado. Total filas: {Count}", data.Count);

        return data;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OvertimePlanMonthlyReportDto>> GetOvertimePlanMonthlyDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _logger.LogInformation(
            "Generando reporte mensual de planificación vs. ejecución de horas extra. Período: {Start} - {End} | DeptId: {DeptId} | EmployeeId: {EmpId}",
            filter.StartDate?.ToString("yyyy-MM-dd") ?? "N/A",
            filter.EndDate?.ToString("yyyy-MM-dd")   ?? "N/A",
            filter.DepartmentId?.ToString() ?? "Todas",
            filter.EmployeeId?.ToString()   ?? "Todos");

        var data = await _repository.GetOvertimePlanMonthlyDataAsync(filter, ct);

        _logger.LogInformation("Reporte mensual de planificación vs. ejecución generado. Total filas: {Count}", data.Count);

        return data;
    }

    private async Task<decimal> GetParameterDecimalAsync(string name, decimal defaultValue, CancellationToken ct)
    {
        var list = await _parametersRepository.GetByNameAsync(name, ct);
        var value = list?.FirstOrDefault(p => p.IsActive)?.Pvalues;
        return decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }
}

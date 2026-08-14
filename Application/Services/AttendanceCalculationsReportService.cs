using Microsoft.Extensions.Logging;
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

    private async Task<decimal> GetParameterDecimalAsync(string name, decimal defaultValue, CancellationToken ct)
    {
        var list = await _parametersRepository.GetByNameAsync(name, ct);
        var value = list?.FirstOrDefault(p => p.IsActive)?.Pvalues;
        return decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : defaultValue;
    }
}

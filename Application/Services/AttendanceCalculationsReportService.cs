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
    private readonly IAttendanceCalculationsReportRepository _repository;
    private readonly ILogger<AttendanceCalculationsReportService> _logger;

    public AttendanceCalculationsReportService(
        IAttendanceCalculationsReportRepository repository,
        ILogger<AttendanceCalculationsReportService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
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
}

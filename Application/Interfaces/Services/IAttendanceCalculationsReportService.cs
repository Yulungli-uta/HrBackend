using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Servicio de negocio para los reportes basados en <c>HR.tbl_AttendanceCalculations</c>.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: este servicio tiene una única responsabilidad — orquestar
/// las consultas de reportes de cálculos de asistencia y aplicar reglas de negocio
/// (validaciones de filtros, valores por defecto, etc.).
/// </para>
/// <para>
/// Principio DIP: los <c>IReportSource</c> dependen de esta interfaz,
/// no de la implementación concreta del repositorio.
/// </para>
/// </remarks>
public interface IAttendanceCalculationsReportService
{
    /// <summary>
    /// Obtiene los datos de atrasos para el período y filtros indicados.
    /// </summary>
    /// <param name="filter">Filtros del reporte.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyList<LatenessReportDto>> GetLatenessDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene los datos de horas extras para el período y filtros indicados.
    /// </summary>
    /// <param name="filter">Filtros del reporte.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyList<OvertimeReportDto>> GetOvertimeDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el reporte cruzado de asistencia para el período y filtros indicados.
    /// </summary>
    /// <param name="filter">Filtros del reporte.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyList<AttendanceCrossReportDto>> GetAttendanceCrossDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);
}

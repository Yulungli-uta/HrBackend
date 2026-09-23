using WsUtaSystem.Application.DTOs.Common;
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

    /// <summary>
    /// Obtiene el detalle de marcaciones para el reporte de asistencia básico.
    /// </summary>
    /// <param name="filter">Filtros del reporte.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyList<AttendanceReportDto>> GetAttendanceDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el reporte consolidado de subsidio de alimentación: días efectivamente
    /// laborados por empleado en el período, multiplicados por el valor diario
    /// parametrizado en <c>HR.tbl_Parameters</c> (<c>FOOD_SUBSIDY_DAILY_VALUE</c>).
    /// </summary>
    /// <param name="filter">Filtros del reporte.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyList<FoodSubsidySummaryReportDto>> GetFoodSubsidySummaryDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el reporte de subsidio de alimentación agrupado por empleado y horario
    /// trabajado (sin fecha individual): jornadas calificadas por cada horario distinto
    /// que el empleado cumplió en el período, multiplicadas por el valor diario
    /// parametrizado (<c>FOOD_SUBSIDY_DAILY_VALUE</c>). Ordenado por nombre de empleado.
    /// </summary>
    /// <param name="filter">Filtros del reporte.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyList<FoodSubsidyByScheduleReportDto>> GetFoodSubsidyByScheduleDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el reporte de novedades de asistencia para el período y filtros indicados,
    /// para todo el personal.
    /// </summary>
    /// <param name="filter">Filtros del reporte.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyList<AttendanceNoveltyReportDto>> GetAttendanceNoveltiesDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Igual que <see cref="GetAttendanceNoveltiesDataAsync"/> pero paginado, para la
    /// pantalla interactiva de novedades (con SearchText y NoveltyType).
    /// </summary>
    /// <param name="filter">Filtros del reporte (incluye SearchText y NoveltyType).</param>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Registros por página.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<PagedResult<AttendanceNoveltyReportDto>> GetAttendanceNoveltiesSummaryAsync(
        ReportFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el conteo de días con atraso por empleado (una fila por empleado) para el
    /// período y filtros indicados — usado por la pantalla de resumen de atrasos.
    /// </summary>
    /// <param name="filter">Filtros del reporte (incluye SearchText para cédula/nombre parcial).</param>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Registros por página.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<PagedResult<LatenessSummaryReportDto>> GetLatenessSummaryDataAsync(
        ReportFilterDto filter,
        int page,
        int pageSize,
        CancellationToken ct = default);
}

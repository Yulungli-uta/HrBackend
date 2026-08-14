using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>
/// Repositorio especializado para las consultas de reportes sobre
/// <c>HR.tbl_AttendanceCalculations</c>.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: este repositorio tiene una única responsabilidad — proyectar
/// los datos de cálculos de asistencia hacia los DTOs requeridos por cada reporte.
/// No realiza escrituras ni gestiona el ciclo de vida de las entidades.
/// </para>
/// <para>
/// Principio ISP: se separa del <see cref="IAttendanceCalculationsRepository"/>
/// (que maneja el CRUD genérico) para no contaminar ese contrato con métodos
/// de solo lectura orientados a reportes.
/// </para>
/// <para>
/// Todas las consultas usan <c>AsNoTracking()</c> para maximizar el rendimiento
/// en operaciones de solo lectura.
/// </para>
/// </remarks>
public interface IAttendanceCalculationsReportRepository
{
    /// <summary>
    /// Obtiene los datos de atrasos por empleado en el rango de fechas indicado.
    /// Solo retorna registros con <c>MinutesLate &gt; 0</c> o <c>TardinessMin &gt; 0</c>.
    /// </summary>
    /// <param name="filter">Filtros del reporte: StartDate, EndDate, EmployeeId, DepartmentId.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Colección proyectada de <see cref="LatenessReportDto"/>.</returns>
    Task<IReadOnlyList<LatenessReportDto>> GetLatenessDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene los datos de horas extras (ordinarias, nocturnas, feriado y fuera de horario)
    /// por empleado en el rango de fechas indicado.
    /// Solo retorna registros con al menos un tipo de minuto extra mayor a cero.
    /// </summary>
    /// <param name="filter">Filtros del reporte: StartDate, EndDate, EmployeeId, DepartmentId.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Colección proyectada de <see cref="OvertimeReportDto"/>.</returns>
    Task<IReadOnlyList<OvertimeReportDto>> GetOvertimeDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el reporte cruzado de asistencia: horas trabajadas, permisos, vacaciones,
    /// justificaciones y licencias médicas consolidadas por empleado/día.
    /// </summary>
    /// <param name="filter">Filtros del reporte: StartDate, EndDate, EmployeeId, DepartmentId.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Colección proyectada de <see cref="AttendanceCrossReportDto"/>.</returns>
    Task<IReadOnlyList<AttendanceCrossReportDto>> GetAttendanceCrossDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el detalle de marcaciones (entrada/salida/horas/estado) por empleado y día,
    /// para el reporte de asistencia básico.
    /// </summary>
    /// <param name="filter">Filtros del reporte: StartDate, EndDate, EmployeeId, DepartmentId.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyList<AttendanceReportDto>> GetAttendanceDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene el consolidado de días efectivamente laborados por empleado (suma de
    /// <c>FoodSubsidy = 1</c>) en el rango de fechas indicado, para el reporte de
    /// subsidio de alimentación. No filtra por tipo de contrato/régimen: el flag
    /// <c>FoodSubsidy</c> ya solo se activa para personal de Código de Trabajo, así
    /// que el resto de empleados aparece naturalmente con 0 días y queda excluido.
    /// Solo incluye empleados con al menos un día con subsidio (&gt; 0).
    /// </summary>
    /// <param name="filter">
    /// Filtros del reporte: StartDate, EndDate, DepartmentId, EmployeeId,
    /// Identification (cédula) y LaborRegimeId (todos opcionales).
    /// </param>
    /// <param name="ct">Token de cancelación.</param>
    Task<IReadOnlyList<FoodSubsidySummaryReportDto>> GetFoodSubsidySummaryDataAsync(
        ReportFilterDto filter,
        CancellationToken ct = default);
}

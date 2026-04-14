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
}

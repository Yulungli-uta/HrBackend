namespace WsUtaSystem.Reports.Core;

/// <summary>
/// Identifica de forma segura y sin ambigüedad cada tipo de reporte disponible en el sistema.
/// </summary>
/// <remarks>
/// Principio OCP: para agregar un nuevo reporte basta con añadir un valor a este enum
/// y crear su correspondiente <c>IReportSource</c>. No se modifica ningún código existente.
/// </remarks>
public enum ReportType
{
    /// <summary>Reporte de empleados activos e inactivos.</summary>
    Employees = 1,
    /// <summary>Reporte de registros de asistencia (entradas y salidas).</summary>
    Attendance = 2,
    /// <summary>Reporte de estructura y estadísticas de departamentos.</summary>
    Departments = 3,
    /// <summary>Reporte de resumen consolidado de asistencia por empleado.</summary>
    AttendanceSummary = 4,
    /// <summary>Reporte detallado de empleados filtrado por dependencia.</summary>
    EmployeesByDepartment = 5,
    /// <summary>Reporte resumen agrupado por dependencia y tipo de contrato.</summary>
    DepartmentContractSummary = 6,
    /// <summary>Reporte resumen agrupado por horario asignado y tipo de contrato.</summary>
    ScheduleContractSummary = 7,
    /// <summary>Reporte de atrasos por empleado en un período determinado.</summary>
    Lateness = 8,
    /// <summary>Reporte de horas extras (ordinarias, nocturnas, feriado y fuera de horario).</summary>
    Overtime = 9,
    /// <summary>Reporte cruzado de asistencia: horas trabajadas, permisos, vacaciones, justificaciones y licencias.</summary>
    AttendanceCross = 10,

    // ── Reportes de Gestión RH ────────────────────────────────────────────────
    /// <summary>Reporte de contratos (todos los estados con filtro).</summary>
    Contracts = 11,
    /// <summary>Reporte de contratos vigentes a la fecha actual.</summary>
    ActiveContracts = 12,
    /// <summary>Reporte de acciones de personal (todas las categorías con filtro).</summary>
    PersonnelActions = 13,
    /// <summary>Reporte de acciones vigentes (solo movimientos/ingresos/económicos activos hoy).</summary>
    ActivePersonnelActions = 14,
    /// <summary>Reporte histórico por empleado: contratos y acciones de cambio de puesto.</summary>
    EmployeeHistory = 15,
    /// <summary>Reporte de permisos otorgados.</summary>
    GrantedPermissions = 16,
    /// <summary>Reporte de solicitudes de contrato.</summary>
    ContractRequests = 17,
    /// <summary>Reporte de certificaciones financieras.</summary>
    Certifications = 18,

    // ── Reportes del módulo Guardias ─────────────────────────────────────────
    /// <summary>Reporte detallado de planificación de turnos por guardia, fecha, ubicación y estado.</summary>
    GuardShiftPlanning = 19,
    /// <summary>Reporte de cobertura por ubicación: cuántos guardias cubren cada punto en cada turno y fecha.</summary>
    GuardLocationCoverage = 20,
    /// <summary>Reporte de cambios de turno, reemplazos y ausencias.</summary>
    GuardShiftChanges = 21,
    /// <summary>Reporte de guardias por grupo con su ubicación asignada en el periodo de rotación activo.</summary>
    GuardGroupRoster = 22,
    /// <summary>Cronograma imprimible en formato matriz: filas = guardias, columnas = fechas, celdas = turno.</summary>
    GuardScheduleMatrix = 23
}

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
    AttendanceSummary = 4
}

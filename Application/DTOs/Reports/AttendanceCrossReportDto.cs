namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO de proyección para el reporte cruzado de asistencia.
/// Consolida en una sola fila por empleado/día: horas trabajadas, permisos,
/// vacaciones, justificaciones y licencias médicas.
/// Los datos provienen de <c>HR.tbl_AttendanceCalculations</c> que ya tiene
/// toda esta información consolidada en un solo lugar.
/// </summary>
/// <remarks>
/// Este reporte es el más completo del sistema: permite al área de RRHH ver
/// en una sola vista el comportamiento de asistencia de cada empleado,
/// cruzando tiempo trabajado con todas las novedades del día.
/// </remarks>
public sealed record AttendanceCrossReportDto
{
    /// <summary>ID del empleado.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Número de cédula del empleado.</summary>
    public string IdCard { get; init; } = string.Empty;

    /// <summary>Nombre completo del empleado (FirstName + LastName).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Fecha de trabajo.</summary>
    public DateOnly WorkDate { get; init; }

    // ── Tiempo trabajado ─────────────────────────────────────────────────────

    /// <summary>Total de minutos trabajados en el día.</summary>
    public int TotalWorkedMinutes { get; init; }

    /// <summary>Minutos de horario regular trabajados.</summary>
    public int RegularMinutes { get; init; }

    /// <summary>Minutos de horas extras.</summary>
    public int OvertimeMinutes { get; init; }

    /// <summary>Minutos programados para ese día.</summary>
    public int ScheduledMinutes { get; init; }

    /// <summary>Minutos de ausencia (diferencia entre programado y trabajado).</summary>
    public int AbsentMinutes { get; init; }

    // ── Novedades del día ────────────────────────────────────────────────────

    /// <summary>Minutos cubiertos por permisos aprobados.</summary>
    public int PermissionMinutes { get; init; }

    /// <summary>Indica si el empleado tiene permiso registrado en este día.</summary>
    public bool HasPermission { get; init; }

    /// <summary>Minutos cubiertos por vacaciones.</summary>
    public int VacationMinutes { get; init; }

    /// <summary>Indica si el empleado tiene vacaciones registradas en este día.</summary>
    public bool HasVacation { get; init; }

    /// <summary>Minutos justificados (marcaciones justificadas).</summary>
    public int JustificationMinutes { get; init; }

    /// <summary>Indica si el empleado tiene justificación aprobada en este día.</summary>
    public bool HasJustification { get; init; }

    /// <summary>Minutos cubiertos por licencia médica.</summary>
    public int MedicalLeaveMinutes { get; init; }

    /// <summary>Indica si el empleado tiene licencia médica en este día.</summary>
    public bool HasMedicalLeave { get; init; }

    /// <summary>Minutos de licencia con goce de sueldo.</summary>
    public int PaidLeaveMinutes { get; init; }

    /// <summary>Minutos de licencia sin goce de sueldo.</summary>
    public int UnpaidLeaveMinutes { get; init; }

    /// <summary>Minutos de vacaciones descontadas del saldo.</summary>
    public int VacationDeductedMinutes { get; init; }

    /// <summary>Minutos recuperados (tiempo de recuperación aprobado).</summary>
    public int RecoveredMinutes { get; init; }

    // ── Atrasos ──────────────────────────────────────────────────────────────

    /// <summary>Minutos de tardanza neta.</summary>
    public int TardinessMin { get; init; }

    /// <summary>Minutos de salida anticipada.</summary>
    public int EarlyLeaveMinutes { get; init; }

    // ── Subsidio ─────────────────────────────────────────────────────────────

    /// <summary>Subsidio de alimentación aplicado (en minutos o unidades según configuración).</summary>
    public int FoodSubsidy { get; init; }

    // ── Estado ───────────────────────────────────────────────────────────────

    /// <summary>Estado del cálculo (p.ej. PROCESSED, ABSENT, HOLIDAY).</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Indica si se aplicó algún ajuste manual al cálculo.</summary>
    public bool HasManualAdjustment { get; init; }

    /// <summary>Versión del cálculo (útil para auditoría).</summary>
    public int CalculationVersion { get; init; }
}

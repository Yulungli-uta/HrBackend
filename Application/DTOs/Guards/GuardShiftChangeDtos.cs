namespace WsUtaSystem.Application.DTOs.Guards;

public record GuardShiftChangeDto(
    int ShiftChangeId,
    int PlanningId,
    DateOnly WorkDate,
    int OriginalEmployeeId,
    string OriginalEmployeeFullName,
    int? ReplacementEmployeeId,
    string? ReplacementEmployeeFullName,
    int OriginalScheduleId,
    string OriginalScheduleDescription,
    int? NewScheduleId,
    string? NewScheduleDescription,
    string ChangeType,
    string Status,
    bool IsActiveForAttendance,
    string Reason,
    DateTime RequestedAt,
    int? RequestedBy,
    string? RequestedByName,
    int? ApprovedBy,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    string? RejectionReason,
    DateOnly? NewWorkDate,
    int? NewLocationId,
    string? NewLocationName
);

public record CreateGuardShiftReplacementDto(
    int PlanningId,
    int ReplacementEmployeeId,
    int ChangeTypeId,
    string Reason,
    int? NewScheduleId
);

/// <summary>Reasigna el turno del mismo guardia titular a otra fecha/horario/ubicación.
/// Se aplica de inmediato (no pasa por aprobación) y queda registrado como GuardShiftChange
/// tipo REASSIGNMENT para trazabilidad.</summary>
public record CreateGuardShiftReassignmentDto(
    int PlanningId,
    DateOnly NewWorkDate,
    int NewLocationId,
    int NewScheduleId,
    string Reason,
    bool OverrideConflict = false
);

/// <summary>
/// Repite la misma reasignación (mismo desplazamiento de fecha/ubicación/horario) durante
/// varias semanas: para cada semana busca el turno activo del mismo empleado en la fecha
/// original equivalente (+7*n días desde el turno de <see cref="PlanningId"/>) y le aplica
/// la reasignación. Si esa semana no tiene turno activo en la fecha original, se omite.
/// </summary>
public record CreateRecurringGuardShiftReassignmentDto(
    int PlanningId,
    DateOnly NewWorkDate,
    int NewLocationId,
    int NewScheduleId,
    string Reason,
    int RepeatWeeks,
    bool OverrideConflict = false
);

public record ApproveGuardShiftChangeDto(
    string? Notes
);

public record RejectGuardShiftChangeDto(
    string RejectionReason
);

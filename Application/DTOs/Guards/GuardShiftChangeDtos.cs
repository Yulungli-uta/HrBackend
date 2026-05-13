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
    string? RejectionReason
);

public record CreateGuardShiftReplacementDto(
    int PlanningId,
    int ReplacementEmployeeId,
    int ChangeTypeId,
    string Reason,
    int? NewScheduleId
);

public record ApproveGuardShiftChangeDto(
    string? Notes
);

public record RejectGuardShiftChangeDto(
    string RejectionReason
);

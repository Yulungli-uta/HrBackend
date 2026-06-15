namespace WsUtaSystem.Application.DTOs.Guards;

// ─── Plan de vacaciones de guardias ──────────────────────────────────────────

public record GuardVacationPlanDto(
    int      GuardVacationPlanId,
    int      EmployeeId,
    string   EmployeeFullName,
    string?  EmployeeIdCard,
    int      VacationYear,
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate,
    int      StatusTypeId,
    string   StatusName,
    int?     DirectionApprovedBy,
    string?  DirectionApproverName,
    DateTime? DirectionApprovedAt,
    int?     SubmittedToDirectionBy,
    string?  SubmittedByName,
    DateTime? SubmittedToDirectionAt,
    string?  Notes
);

public record CreateGuardVacationPlanDto(
    int      EmployeeId,
    int      VacationYear,
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate,
    string?  Notes
);

public record UpdateGuardVacationPlanDto(
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate,
    string?  Notes
);

public record ApproveGuardVacationPlanDto(
    string? Notes
);

public record RejectGuardVacationPlanDto(
    string Reason
);

// ─── Solicitudes de vacaciones de guardias ───────────────────────────────────

public record GuardVacationRequestDto(
    int      GuardVacationRequestId,
    int      EmployeeId,
    string   EmployeeFullName,
    string?  EmployeeIdCard,
    int?     GuardVacationPlanId,
    int?     VacationId,
    string   RequestType,
    DateOnly OriginalStartDate,
    DateOnly OriginalEndDate,
    DateOnly? RequestedStartDate,
    DateOnly? RequestedEndDate,
    int      SourceYear,
    int?     TargetYear,
    string   Reason,
    string   Status,
    int?     RequestedBy,
    string?  RequestedByName,
    DateTime RequestedAt,
    int?     DirectionApprovedBy,
    string?  DirectionApproverName,
    DateTime? DirectionApprovedAt,
    int?     SubmittedToDirectionBy,
    string?  SubmittedByName,
    DateTime? SubmittedToDirectionAt,
    string?  RejectionReason,
    DateTime? RejectedAt
);

public record SubmitToDirectionDto(
    string? Notes
);

public record CreateChangeDatesRequestDto(
    int      EmployeeId,
    int?     GuardVacationPlanId,
    DateOnly OriginalStartDate,
    DateOnly OriginalEndDate,
    DateOnly RequestedStartDate,
    DateOnly RequestedEndDate,
    int      SourceYear,
    string   Reason
);

public record CreateAccumulateRequestDto(
    int      EmployeeId,
    int?     GuardVacationPlanId,
    DateOnly OriginalStartDate,
    DateOnly OriginalEndDate,
    int      SourceYear,
    int      TargetYear,
    string   Reason
);

public record ApproveGuardVacationRequestDto(
    string? Notes
);

public record RejectGuardVacationRequestDto(
    string Reason
);

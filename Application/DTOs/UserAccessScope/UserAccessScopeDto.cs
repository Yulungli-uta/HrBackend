namespace WsUtaSystem.Application.DTOs.UserAccessScope;

public class UserAccessScopeDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeEmail { get; set; }

    public int ModuleTypeId { get; set; }
    public string? ModuleTypeName { get; set; }

    public int ScopeTypeId { get; set; }
    public string? ScopeTypeName { get; set; }

    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public bool IsActive { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? AssignedBy { get; set; }
    public string? Reason { get; set; }
}

public class UserAccessScopeCreateDto
{
    public int EmployeeId { get; set; }
    public int ModuleTypeId { get; set; }
    public int ScopeTypeId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Reason { get; set; }
}

public class UserAccessScopeUpdateDto
{
    public int ScopeTypeId { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Reason { get; set; }
}

public class UserAccessScopeHistoryDto
{
    public long Id { get; set; }
    public int EmployeeId { get; set; }
    public int ModuleTypeId { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public int? PreviousScopeTypeId { get; set; }
    public int? PreviousDepartmentId { get; set; }
    public int? NewScopeTypeId { get; set; }
    public int? NewDepartmentId { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? ChangeReason { get; set; }
    public DateTime ChangeDateTime { get; set; }
}

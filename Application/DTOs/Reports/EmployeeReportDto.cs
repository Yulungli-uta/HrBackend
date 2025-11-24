namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO para reporte de empleados
/// </summary>
public record EmployeeReportDto
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? MiddleName { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string IdentificationNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string DepartmentCode { get; init; } = string.Empty;
    public string FacultyName { get; init; } = string.Empty;
    public string EmployeeType { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal BaseSalary { get; init; }
    public decimal NetSalary { get; init; }
    public string? ContractType { get; init; }
    public DateTime? ContractStartDate { get; init; }
    public DateTime? ContractEndDate { get; init; }
    public DateTime HireDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

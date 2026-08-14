namespace WsUtaSystem.Application.DTOs.Reports;

/// <summary>
/// DTO para reporte de departamentos
/// </summary>
public record DepartmentReportDto
{
    public int Id { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public string DepartmentCode { get; init; } = string.Empty;
    public string FacultyName { get; init; } = string.Empty;
    public string FacultyCode { get; init; } = string.Empty;
    /// <summary>Tipo de dependencia (ref_Types categoría DEPARTMENT_TYPE: Facultad, Dirección, etc.).</summary>
    public string? DepartmentTypeName { get; init; }
    /// <summary>Ámbito de la dependencia (ref_Types categoría DEPARTMENT_SCOPE: Académico, Administrativo, etc.).</summary>
    public string? DepartmentScopeName { get; init; }
    /// <summary>Nombre de la dependencia padre (tbl_Departments.ParentId). Null = dependencia raíz.</summary>
    public string? ParentDepartmentName { get; init; }
    public bool IsActive { get; init; }
    public int TotalEmployees { get; init; }
    public int ActiveEmployees { get; init; }
    public int InactiveEmployees { get; init; }
    public decimal AverageSalary { get; init; }
    public decimal TotalSalaries { get; init; }
    public decimal MinSalary { get; init; }
    public decimal MaxSalary { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

}

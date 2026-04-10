namespace WsUtaSystem.Application.DTOs.DepartmentAuthority;

/// <summary>
/// DTO de respuesta para la consulta de denominación de autoridad por cédula de identidad.
/// Retorna la información de la autoridad activa actual del empleado identificado por su cédula.
/// </summary>
public class DepartmentAuthorityDenominationDto
{
    /// <summary>Cédula de identidad consultada.</summary>
    public string IdCard { get; set; } = null!;

    /// <summary>ID del empleado encontrado.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Nombre completo del empleado.</summary>
    public string EmployeeFullName { get; set; } = null!;

    /// <summary>Email del empleado.</summary>
    public string? EmployeeEmail { get; set; }

    /// <summary>ID de la autoridad de departamento activa.</summary>
    public int? AuthorityId { get; set; }

    /// <summary>Denominación oficial del nombramiento activo.</summary>
    public string? Denomination { get; set; }

    /// <summary>Nombre del tipo de autoridad.</summary>
    public string? AuthorityTypeName { get; set; }

    /// <summary>Nombre del departamento en el que ejerce la autoridad.</summary>
    public string? DepartmentName { get; set; }

    /// <summary>Código del departamento.</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>Fecha de inicio de la vigencia de la autoridad activa.</summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>Código de resolución del nombramiento activo.</summary>
    public string? ResolutionCode { get; set; }

    /// <summary>Indica si el empleado tiene una autoridad activa en algún departamento.</summary>
    public bool HasActiveAuthority { get; set; }
}

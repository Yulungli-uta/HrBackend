namespace WsUtaSystem.Application.DTOs.DepartmentAuthority;

/// <summary>
/// DTO de lectura para una autoridad de departamento.
/// Incluye los nombres resueltos de todas las FK para evitar N+1 en el cliente.
/// </summary>
public class DepartmentAuthorityDto
{
    /// <summary>Identificador único del registro.</summary>
    public int AuthorityId { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Claves foráneas (IDs)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>ID del departamento.</summary>
    public int DepartmentId { get; set; }

    /// <summary>ID del empleado.</summary>
    public int EmployeeId { get; set; }

    /// <summary>ID del tipo de autoridad.</summary>
    public int AuthorityTypeId { get; set; }

    /// <summary>ID del cargo (opcional).</summary>
    public int? JobId { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Nombres resueltos de las FK (enriquecimiento del DTO)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Nombre completo del departamento.</summary>
    public string? DepartmentName { get; set; }

    /// <summary>Nombre completo del empleado (FirstName + LastName de People).</summary>
    public string? EmployeeFullName { get; set; }

    /// <summary>Cédula de identidad del empleado.</summary>
    public string? EmployeeIdCard { get; set; }

    /// <summary>Email del empleado.</summary>
    public string? EmployeeEmail { get; set; }

    /// <summary>Nombre del tipo de autoridad (ej. "Decano", "Director").</summary>
    public string? AuthorityTypeName { get; set; }

    /// <summary>Descripción del cargo asociado (opcional).</summary>
    public string? JobDescription { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Datos propios del registro
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Denominación oficial del nombramiento.</summary>
    public string? Denomination { get; set; }

    /// <summary>Fecha de inicio de vigencia.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Fecha de fin de vigencia. NULL = aún activa.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Código o número de resolución de respaldo.</summary>
    public string? ResolutionCode { get; set; }

    /// <summary>Observaciones adicionales.</summary>
    public string? Notes { get; set; }

    /// <summary>Indica si el registro está activo.</summary>
    public bool IsActive { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Auditoría
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Fecha de creación del registro.</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>ID del usuario que creó el registro.</summary>
    public int? CreatedBy { get; set; }

    /// <summary>Fecha de última actualización.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>ID del usuario que realizó la última actualización.</summary>
    public int? UpdatedBy { get; set; }
}

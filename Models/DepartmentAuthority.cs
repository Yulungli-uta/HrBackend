using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Entidad que representa una autoridad asignada a un departamento.
/// Mapea la tabla HR.tbl_DepartmentAuthorities.
/// </summary>
public class DepartmentAuthority : IAuditable
{
    /// <summary>Identificador único de la autoridad de departamento.</summary>
    public int AuthorityId { get; set; }

    /// <summary>Identificador del departamento al que pertenece la autoridad.</summary>
    public int DepartmentId { get; set; }

    /// <summary>Identificador del empleado designado como autoridad.</summary>
    public int EmployeeId { get; set; }

    /// <summary>Identificador del tipo de autoridad (FK a HR.ref_Types con Category = 'AUTHORITY_TYPE').</summary>
    public int AuthorityTypeId { get; set; }

    /// <summary>Identificador del cargo asociado (opcional).</summary>
    public int? JobId { get; set; }

    /// <summary>Denominación oficial del cargo de autoridad (ej. "Decano", "Director de Carrera").</summary>
    public string? Denomination { get; set; }

    /// <summary>Fecha de inicio de la vigencia de la autoridad.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Fecha de fin de la vigencia. NULL indica que la autoridad sigue activa.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>Código o número de la resolución que respalda el nombramiento.</summary>
    public string? ResolutionCode { get; set; }

    /// <summary>Observaciones adicionales sobre el nombramiento.</summary>
    public string? Notes { get; set; }

    /// <summary>Indica si el registro está activo en el sistema.</summary>
    public bool IsActive { get; set; } = true;

    // ─────────────────────────────────────────────────────────────────────────
    // Auditoría (IAuditable)
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public int? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime? CreatedAt { get; set; }

    /// <inheritdoc/>
    public int? UpdatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Token de control de concurrencia optimista (TIMESTAMP de SQL Server).</summary>
    public byte[]? RowVersion { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Propiedades de navegación (lazy-safe, no requeridas por EF Core)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Departamento al que pertenece la autoridad.</summary>
    public virtual Departments? Department { get; set; }

    /// <summary>Empleado designado como autoridad.</summary>
    public virtual Employees? Employee { get; set; }

    /// <summary>Tipo de autoridad desde la tabla de referencia.</summary>
    public virtual RefTypes? AuthorityType { get; set; }

    /// <summary>Cargo asociado al nombramiento (opcional).</summary>
    public virtual Job? Job { get; set; }
}

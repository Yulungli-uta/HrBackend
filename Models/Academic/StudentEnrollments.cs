namespace WsUtaSystem.Models.Academic;

/// <summary>
/// Registro de matrícula de un estudiante por período académico.
/// Tabla detalle: la tabla principal Students nunca cambia cuando inicia un nuevo período.
/// </summary>
public class StudentEnrollments
{
    public int EnrollmentId { get; set; }

    public int StudentId { get; set; }

    /// <summary>Código único del período académico, ej: "2024-I", "2024-II", "2025-I".</summary>
    public string PeriodCode { get; set; } = string.Empty;

    public DateOnly EnrollmentDate { get; set; }

    /// <summary>Estado de la matrícula: Activo, Retirado, Egresado, etc.</summary>
    public string Status { get; set; } = "Activo";

    /// <summary>Carrera o programa académico.</summary>
    public string? Program { get; set; }

    /// <summary>Facultad o unidad académica.</summary>
    public string? Faculty { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Propiedades de navegación ──────────────────────────────────────────────

    public virtual Students? Student { get; set; }
}

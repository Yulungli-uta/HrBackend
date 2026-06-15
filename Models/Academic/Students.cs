using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Academic;

public class Students : IAuditable
{
    public int StudentId { get; set; }

    /// <summary>FK → HR.tbl_People.PersonId (soft cross-table — misma BD).</summary>
    public int PersonID { get; set; }

    /// <summary>TypeId de HR.ref_Types que identifica la categoría del estudiante (ej: Pregrado, Posgrado).</summary>
    public int StudentTypeId { get; set; }

    /// <summary>Correo institucional asignado. Null hasta que sea aprovisionado en AD.</summary>
    public string? InstitutionalEmail { get; set; }

    /// <summary>Código del estudiante en el sistema académico externo.</summary>
    public string? ExternalStudentCode { get; set; }

    public bool IsActive { get; set; } = true;

    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Propiedades de navegación ──────────────────────────────────────────────

    /// <summary>Persona asociada al estudiante (join por PersonID → PersonId).</summary>
    public virtual People? People { get; set; }

    public virtual ICollection<StudentEnrollments> Enrollments { get; set; } = [];
}

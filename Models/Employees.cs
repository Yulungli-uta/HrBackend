
using WsUtaSystem.Application.Interfaces.Auditable;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Models;

public class Employees : IAuditable, ISoftDeletable
{
    public int EmployeeId { get; set; }
    public int PersonID { get; set; }
    /// <summary>Nullable porque la columna en base de datos lo es — un empleado sin este dato
    /// asignado no debe tumbar ninguna consulta.</summary>
    public int? EmployeeType { get; set; }
    public int? DepartmentId { get; set; }
    public int? ImmediateBossId { get; set; }
    public int? JobId { get; set; }

    /// <summary>FK -> ref_Types (Category='SIIES_TIPO_DOCENTE_LOES'). NULL/"No Aplica" salvo Job.SiiesTipoFuncionarioTypeId = DOCENTE LOES.</summary>
    public int? TipoDocenteLoesTypeId { get; set; }

    /// <summary>FK -> ref_Types (Category='SIIES_CATEGORIA_DOCENTE_LOES'). NULL/"No Aplica" salvo Job.SiiesTipoFuncionarioTypeId = DOCENTE LOES.</summary>
    public int? CategoriaDocenteLoesTypeId { get; set; }

    /// <summary>FK -> ref_Types (Category='BUDGET_UNIT'). Partida presupuestaria de la que sale el sueldo del empleado — distinta de Department (unidad organizacional).</summary>
    public int? BudgetUnitTypeId { get; set; }

    public DateOnly HireDate { get; set; }

    /// <summary>Fecha de antigüedad para beneficios que no necesariamente coincide con HireDate
    /// (ej. bono vacacional de Código de Trabajo a los 5 años, calculado desde la fecha de
    /// contrato indefinido en vez de la fecha de ingreso). NULL = usar HireDate como respaldo.</summary>
    public DateOnly? SeniorityDate { get; set; }

    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Propiedad de navegación (no cargada por defecto — requiere Include explícito)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Persona asociada al empleado (join por PersonID → PersonId).</summary>
    public virtual People? People { get; set; }

    public virtual Job? Job { get; set; }
}


using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Régimen laboral (LOSEP/LOES/CT) vigente o histórico de un empleado.
/// Un empleado puede tener varias filas activas simultáneas (uno por régimen),
/// cada una atada al contrato o acción de personal que la originó.
/// Mapea la tabla HR.tbl_EmployeeLaborRegime.
/// </summary>
public class EmployeeLaborRegime : IAuditable
{
    public int Id { get; set; }

    /// <summary>FK -> HR.tbl_Employees.</summary>
    public int EmployeeId { get; set; }

    /// <summary>FK -> HR.ref_Types (Category='CONTRACT_TYPE'). 57=LOSEP, 58=LOES, 59=Código Trabajo.</summary>
    public int LaborRegimeId { get; set; }

    /// <summary>Departamento/Facultad donde se ejerce este régimen. FK -> HR.tbl_Departments.</summary>
    public int? DepartmentId { get; set; }

    /// <summary>Cargo asociado a este régimen. FK -> HR.tbl_Job.</summary>
    public int? JobId { get; set; }

    /// <summary>true = nombramiento (fijo/provisional, sin vencimiento); false = régimen temporal atado a un contrato.</summary>
    public bool IsIndefinite { get; set; }

    /// <summary>'CONTRACT' | 'PERSONNEL_ACTION'. Origen documental de este régimen.</summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Número de contrato o de acción de personal que originó el régimen (denormalizado para lectura rápida).</summary>
    public string? DocumentNumber { get; set; }

    /// <summary>FK -> HR.tbl_Contracts, cuando DocumentType='CONTRACT'.</summary>
    public int? SourceContractId { get; set; }

    /// <summary>FK -> HR.tbl_PersonnelAction, cuando DocumentType='PERSONNEL_ACTION'.</summary>
    public int? SourcePersonnelActionId { get; set; }

    /// <summary>Fecha de activación del régimen.</summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>Fecha de desactivación. NULL mientras el régimen sigue activo.</summary>
    public DateOnly? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>SIIES INGRESO_POR_CONCURSO. NULL = sin clasificar (no confundir con NO); se completa hacia adelante.</summary>
    public bool? IngresoPorConcurso { get; set; }

    /// <summary>
    /// Calculado automáticamente por <see cref="Application.Interfaces.Services.IEmployeeLaborRegimeService"/>:
    /// gana el régimen con nombramiento (IsIndefinite); si ninguno es nombramiento, gana LOSEP.
    /// Solo un régimen activo por empleado puede ser principal.
    /// </summary>
    public bool IsPrincipal { get; set; }

    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual Employees? Employee { get; set; }
    public virtual RefTypes? LaborRegime { get; set; }
    public virtual Departments? Department { get; set; }
    public virtual Job? Job { get; set; }
}

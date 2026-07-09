using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Define qué departamentos/facultades puede ver o gestionar un usuario,
/// por módulo/trámite (Contratos, Acciones de Personal, u otros trámites
/// futuros vía ref_Types Category='ACCESS_MODULE_TYPE'), evitando que vea
/// datos de toda la institución.
/// </summary>
public class UserAccessScope : IAuditable
{
    public int Id { get; set; }

    /// <summary>FK -> HR.tbl_Employees. En runtime se cruza contra ICurrentUserService.EmployeeId.</summary>
    public int EmployeeId { get; set; }

    /// <summary>FK -> ref_Types Category='ACCESS_MODULE_TYPE' (CONTRACTS, PERSONNEL_ACTIONS, ...).</summary>
    public int ModuleTypeId { get; set; }

    /// <summary>FK -> ref_Types Category='ACCESS_SCOPE_TYPE' (GLOBAL, DEPARTMENT_TREE, DEPARTMENT_ONLY).</summary>
    public int ScopeTypeId { get; set; }

    /// <summary>Departamento/Facultad asignado. NULL únicamente cuando ScopeTypeId = GLOBAL.</summary>
    public int? DepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.Now;
    public DateTime? ExpiresAt { get; set; }
    public string? AssignedBy { get; set; }
    public string? Reason { get; set; }

    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public byte[]? RowVersion { get; set; }

    public virtual RefTypes? ModuleType { get; set; }
    public virtual RefTypes? ScopeType { get; set; }
    public virtual Departments? Department { get; set; }
}

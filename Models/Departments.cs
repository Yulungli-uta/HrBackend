
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class Departments : IAuditable{
    public int DepartmentId { get; set; }
    public int? ParentId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ShortName { get; set; }
    public int? DepartmentType { get; set; }
    public int? DepartmentScope { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public int? DeanDirector { get; set; }
    public string? BudgetCode { get; set; }
    public int? Dlevel { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Rol institucional crítico del departamento para resolución de firmas
    /// (FK a HR.ref_Types con Category = 'DEPARTMENT_INSTITUTIONAL_ROLE': RECTORADO, FINANCE, HUMAN_RESOURCE, etc.).
    /// NULL para la mayoría de departamentos que no cumplen un rol de firma institucional.
    /// </summary>
    public int? InstitutionalRoleTypeId { get; set; }

    /// <summary>Tipo de referencia del rol institucional.</summary>
    public virtual RefTypes? InstitutionalRoleType { get; set; }
}

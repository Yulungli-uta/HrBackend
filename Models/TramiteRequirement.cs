using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Checklist de documentos requeridos por trámite. Mapea la tabla HR.tbl_TramiteRequirements.
/// </summary>
/// <remarks>
/// No incluye mapeo "documento -> qué placeholder de plantilla alimenta": eso vive en código
/// (ver ContractsService.cs), resuelto por nombre de <see cref="RefTypes"/>. Esta entidad es
/// solo el checklist de obligatoriedad, usado para validar antes de generar un documento.
/// </remarks>
public class TramiteRequirement : IAuditable
{
    public int RequirementId { get; set; }

    /// <summary>FK a HR.ref_Types (Category='ACCESS_MODULE_TYPE'): CONTRACTS, PERSONNEL_ACTIONS, ...</summary>
    public int ModuleTypeId { get; set; }

    /// <summary>
    /// Override puntual dentro del módulo (ej. ContractTypeID cuando ModuleTypeId=CONTRACTS).
    /// NULL = aplica a todo el módulo. Polimórfico: no lleva FK física.
    /// </summary>
    public int? SpecificTypeId { get; set; }

    /// <summary>FK a HR.ref_Types (Category='DOCUMENT_TYPE').</summary>
    public int DocumentTypeId { get; set; }

    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public virtual RefTypes? ModuleType { get; set; }
    public virtual RefTypes? DocumentType { get; set; }
}

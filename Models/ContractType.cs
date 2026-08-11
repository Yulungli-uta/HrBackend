using WsUtaSystem.Application.Interfaces.Auditable;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Models
{
    public class ContractType : IAuditable
    {
        public int ContractTypeId { get; set; }
        public int? PersonalContractTypeId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? ContractText { get; set; }
        public string? ContractCode { get; set; }

        /// <summary>FK -> ref_Types (Category='SIIES_RELACION_IES'). Homologación para el reporte SIIES Funcionarios.</summary>
        public int? SiiesRelacionIesTypeId { get; set; }

        /// <summary>Familia documental asociada a este tipo (ref_Types DOCUMENT_TEMPLATE_TYPE).</summary>
        public int? DocumentTemplateTypeId { get; set; }

        /// <summary>Plantilla activa por defecto para generar documentos de este tipo de contrato.</summary>
        public int? DefaultTemplateId { get; set; }

        public DocumentTemplate? DefaultTemplate { get; set; }

        /// <summary>Plantilla alterna a usar cuando el contrato se firma por delegación (Contracts.IsDelegation = true).</summary>
        public int? DelegationTemplateId { get; set; }

        public DocumentTemplate? DelegationTemplate { get; set; }

        /// <summary>Prefijo para numeración de documentos (ej: "CONT-OCAS", "CONT-TITU").</summary>
        public string? NumberingPrefix { get; set; }

        /// <summary>Año del ciclo de numeración actual. Se reinicia la secuencia al cambiar de año.</summary>
        public int NumberingYear { get; set; } = DateTime.Now.Year;

        /// <summary>Último número de secuencia emitido para el año actual.</summary>
        public int NumberingLastSequence { get; set; } = 0;

        // ── Integración Active Directory ─────────────────────────────────────────

        /// <summary>Si verdadero, al vincular este tipo de contrato se debe crear un usuario en AD local.</summary>
        public bool RequiresAdUserCreation { get; set; } = false;

        /// <summary>Si verdadero, al finalizar o revocar este contrato se debe deshabilitar el usuario en AD local.</summary>
        public bool RequiresAdUserDisable { get; set; } = false;

        /// <summary>Si verdadero, al vincular este tipo de contrato se deben asignar grupos/roles en AD local.</summary>
        public bool RequiresAdGroupAssignment { get; set; } = false;

        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

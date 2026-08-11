using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class Job : IAuditable
    {
        public int JobID { get; set; }
        public string? Description { get; set; }
        public int? JobTypeId { get; set; }
        public int? GroupId { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>FK -> ref_Types (Category='SIIES_TIPO_FUNCIONARIO'). Clasificación por cargo para el reporte SIIES Funcionarios.</summary>
        public int? SiiesTipoFuncionarioTypeId { get; set; }

        /// <summary>SIIES PUESTO_JERARQUICO_SUPERIOR. Clasificación por cargo (no por empleado).</summary>
        public bool PuestoJerarquicoSuperior { get; set; }

        /// <summary>Sueldo de referencia/vigente del cargo (moda de los sueldos reales encontrados). No es el sueldo real de ninguna persona en particular — ver HR.vw_JobSalaryDiscrepancy.</summary>
        public decimal? ReferenceSalary { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class OccupationalGroup : IAuditable
    {
        public int GroupId { get; set; }
        public string Description { get; set; } = null!;
        public decimal Rmu { get; set; }
        public int DegreeId { get; set; }
        /// <summary>
        /// Clasificación institucional "Escala UEP" (ref_Types.Category=UEP_SCALE_TYPE), cuando
        /// este grupo ocupacional también se identifica bajo esa nomenclatura en la matriz de
        /// personal. No reemplaza el RMU/GroupID de la escala LOSEP — es solo una etiqueta
        /// adicional de clasificación, ya que los montos UEP coinciden exactamente con los de
        /// Servidor Público/Directivo/Nivel Jerárquico Superior ya existentes.
        /// </summary>
        public int? UepScaleTypeId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class OccupationalGroup : IAuditable
    {
        public int GroupId { get; set; }
        public string Description { get; set; } = null!;
        /// <summary>RMU referencial de la escala (sueldo real = HR.tbl_SalaryHistory). Nulo cuando no hay un monto oficial.</summary>
        public decimal? Rmu { get; set; }
        public int DegreeId { get; set; }
        /// <summary>
        /// Clasificación institucional "Escala UEP" (ref_Types.Category=UEP_SCALE_TYPE), cuando
        /// este grupo ocupacional también se identifica bajo esa nomenclatura en la matriz de
        /// personal. No reemplaza el RMU/GroupID de la escala LOSEP — es solo una etiqueta
        /// adicional de clasificación, ya que los montos UEP coinciden exactamente con los de
        /// Servidor Público/Directivo/Nivel Jerárquico Superior ya existentes.
        /// </summary>
        public int? UepScaleTypeId { get; set; }
        /// <summary>
        /// Familia del grupo (ref_Types.Category=OCCUPATIONAL_GROUP_FAMILY), p. ej. "Personal Académico Titular".
        /// Hasta que RRHH defina los grados de Código de Trabajo, esas escalas no la tienen. Dentro de una familia no se repite el grado (índice único filtrado).
        /// </summary>
        public int? FamilyTypeId { get; set; }
        /// <summary>
        /// Nivel dentro del grupo (p. ej. Servidor Público nivel 5, Personal Académico Auxiliar nivel 1). Es distinto del grado:
        /// el grado es el escalón del catálogo (Grado 7) y el nivel es el número que identifica la escala dentro del grupo.
        /// Nulo cuando el grupo no usa niveles. No se repite dentro de una familia (índice único filtrado).
        /// </summary>
        public int? Level { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

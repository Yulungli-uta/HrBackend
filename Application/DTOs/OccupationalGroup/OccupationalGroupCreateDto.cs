namespace WsUtaSystem.Application.DTOs.OccupationalGroup
{
    public class OccupationalGroupCreateDto
    {
        public int GroupId { get; set; }
        /// <summary>Nombre de la escala. Si se envía la familia, el servidor lo compone como "Grupo [Nivel] N" y este valor se ignora.</summary>
        public string? Description { get; set; }
        public decimal? Rmu { get; set; }
        public int DegreeId { get; set; }
        public int? UepScaleTypeId { get; set; }
        public int? FamilyTypeId { get; set; }
        /// <summary>Nivel dentro del grupo; nulo si el grupo no usa niveles.</summary>
        public int? Level { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        //public DateTime? UpdatedAt { get; set; }
    }
}

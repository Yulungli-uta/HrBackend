namespace WsUtaSystem.Application.DTOs.OccupationalGroup
{
    public class OccupationalGroupDto
    {
        public int GroupId { get; set; }
        public string Description { get; set; } = null!;
        public decimal? Rmu { get; set; }
        public int DegreeId { get; set; }
        public int? UepScaleTypeId { get; set; }
        public int? FamilyTypeId { get; set; }
        /// <summary>Nivel dentro del grupo; nulo si el grupo no usa niveles.</summary>
        public int? Level { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

namespace WsUtaSystem.Models.Views
{
    public class VwJobWithDegreeAndGroup
    {
        public int JobID { get; set; }
        public string? JobDescription { get; set; }
        public string? JobTypeName { get; set; }
        public int? GroupID { get; set; }
        public string? OccupationalGroup { get; set; }
        public decimal? RMU { get; set; }
        public int? DegreeID { get; set; }
        public string? Degree { get; set; }
        public bool? DegreeIsActive { get; set; }
        /// <summary>Clasificación adicional (Escala UEP) del grupo ocupacional; nula si el grupo no la tiene.</summary>
        public string? UepScaleName { get; set; }
        /// <summary>Grupo ocupacional (familia) de la escala; nulo si aún no se clasificó.</summary>
        public int? FamilyTypeId { get; set; }
        public string? FamilyName { get; set; }
        /// <summary>Nivel dentro del grupo (p. ej. 5 en "Servidor Público 5"); nulo si el grupo no usa niveles.</summary>
        public int? ScaleLevel { get; set; }
        /// <summary>Estado del cargo; los selectores no ofrecen cargos inactivos (salvo el ya asignado).</summary>
        public bool? JobIsActive { get; set; }
    }
}

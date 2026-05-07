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
    }
}

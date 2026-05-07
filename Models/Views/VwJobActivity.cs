namespace WsUtaSystem.Models.Views
{
    public class VwJobActivity
    {
        public int JobID { get; set; }
        public string? JobDescription { get; set; }
        public string JobTypeName { get; set; }
        public string OccupationalGroup { get; set; }
        public int ActivitiesID { get; set; }
        public string? ActivityDescription { get; set; }
        public string ActivitiesType { get; set; }
        public bool ActivityAssignmentActive { get; set; }
    }
}

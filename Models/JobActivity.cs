namespace WsUtaSystem.Models
{
    public class JobActivity
    {
        public int ActivitiesId { get; set; }
        public int JobID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

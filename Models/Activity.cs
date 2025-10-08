namespace WsUtaSystem.Models
{
    public class Activity
    {
        public int ActivitiesId { get; set; }
        public string? Description { get; set; }
        public string ActivitiesType { get; set; } = "LABORAL";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

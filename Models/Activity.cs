using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class Activity : IAuditable
    {
        public int ActivitiesId { get; set; }
        public string? Description { get; set; }
        public string ActivitiesType { get; set; } = "LABORAL";
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

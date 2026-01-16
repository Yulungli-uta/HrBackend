using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class JobActivity : IAuditable
    {
        public int ActivitiesId { get; set; }
        public int JobID { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

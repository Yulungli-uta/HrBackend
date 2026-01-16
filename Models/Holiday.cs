using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class Holiday : IAuditable
    {
        public int HolidayID { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime HolidayDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

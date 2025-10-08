namespace WsUtaSystem.Models
{
    public class Holiday
    {
        public int HolidayID { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime HolidayDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

namespace WsUtaSystem.Application.DTOs.Jobs
{
    public class JobDto
    {
        public int JobID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
    }

}

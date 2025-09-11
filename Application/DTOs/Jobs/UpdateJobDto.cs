namespace WsUtaSystem.Application.DTOs.Jobs
{
    public class UpdateJobDto
    {
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }
}

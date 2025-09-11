using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Application.DTOs.Jobs
{
    public class CreateJobDto
    {
        //[Required]
        //[MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}

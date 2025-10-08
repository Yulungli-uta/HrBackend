using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Application.DTOs.Jobs
{
    public class CreateJobDto
    {
        public int JobID { get; set; }
        public string? Description { get; set; }
        public int? JobTypeId { get; set; }
        public int? GroupId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        //public DateTime? UpdatedAt { get; set; }
    }
}

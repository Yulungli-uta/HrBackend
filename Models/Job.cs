using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WsUtaSystem.Models
{
    public class Job
    {
        public int JobID { get; set; }
        public string? Description { get; set; }
        public int? JobTypeId { get; set; }
        public int? GroupId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

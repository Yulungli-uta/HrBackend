using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class Job : IAuditable
    {
        public int JobID { get; set; }
        public string? Description { get; set; }
        public int? JobTypeId { get; set; }
        public int? GroupId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

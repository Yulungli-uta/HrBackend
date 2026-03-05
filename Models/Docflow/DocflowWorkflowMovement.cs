using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Docflow
{
    public class DocflowWorkflowMovement : ICreationAuditable
    {
        public Guid MovementId { get; set; }
        public Guid InstanceId { get; set; }
        public string MovementType { get; set; } = null!; // FORWARD / RETURN
        public string? Comments { get; set; }

        public int? AssignedToUserId { get; set; }

        public int? FromProcessId { get; set; }
        public int? ToProcessId { get; set; }
        public int? FromDepartmentId { get; set; }
        public int? ToDepartmentId { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

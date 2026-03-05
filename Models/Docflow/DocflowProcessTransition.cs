using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Docflow
{
    public class DocflowProcessTransition : IAuditable
    {
        public int TransitionId { get; set; }
        public int FromProcessId { get; set; }
        public int ToProcessId { get; set; }
        public bool IsDefault { get; set; }
        public bool AllowReturn { get; set; }
        public int? ReturnToProcessId { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

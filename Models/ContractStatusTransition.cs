using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class ContractStatusTransition : IAuditable
    {
        public int TransitionID { get; set; }

        public int FromStatusTypeID { get; set; }
        public int ToStatusTypeID { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

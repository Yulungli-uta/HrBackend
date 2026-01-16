using WsUtaSystem.Application.Interfaces.Auditable;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Models
{
    public class ContractType : IAuditable
    {
        public int ContractTypeId { get; set; }
        public int? PersonalContractTypeId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? ContractText { get; set; }
        public string? ContractCode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

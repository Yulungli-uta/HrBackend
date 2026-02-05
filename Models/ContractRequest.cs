using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class ContractRequest : IAuditable
    {
        public int RequestId { get; set; }
        public int? DepartmentId { get; set; }   
        public int? WorkModalityId { get; set; }
        public int NumberOfPeopleToHire { get; set; } = 0;
        public decimal NumberHour { get; set; } = 0;
        public int TotalPeopleHired { get; set; } = 0;
        public string? Observation { get; set; }    
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        public int? Status { get; set; }

    }
}


namespace WsUtaSystem.Models
{
    public class ContractRequest
    {
        public int RequestId { get; set; }
        public int? WorkModalityId { get; set; }
        public int NumberOfPeopleToHire { get; set; } = 0;
        public decimal NumberHour { get; set; } = 0;
        public int TotalPeopleHired { get; set; } = 0;

        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        public int? Status { get; set; }
    }
}


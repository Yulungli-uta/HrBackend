namespace WsUtaSystem.Models
{
    public class ContractRequest
    {
        public int RequestId { get; set; }
        public int? WorkModalityId { get; set; }
        public int NumberTeacher { get; set; } = 0;
        public decimal NumberHour { get; set; } = 0;
        public int? Status { get; set; }
    }
}


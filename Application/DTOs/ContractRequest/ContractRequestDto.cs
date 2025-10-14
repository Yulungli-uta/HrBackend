namespace WsUtaSystem.Application.DTOs.ContractRequest
{
    public class ContractRequestDto
    {
        public int RequestId { get; set; }
        public int? WorkModalityId { get; set; }
        public int NumberTeacher { get; set; }
        public decimal NumberHour { get; set; }
        public int? Status { get; set; }
    }
}


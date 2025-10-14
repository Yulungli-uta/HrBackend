namespace WsUtaSystem.Application.DTOs.ContractRequest
{
    public class ContractRequestCreateDto
    {
        public int? WorkModalityId { get; set; }
        public int NumberTeacher { get; set; } = 0;
        public decimal NumberHour { get; set; } = 0;
        public int? Status { get; set; }
    }
}


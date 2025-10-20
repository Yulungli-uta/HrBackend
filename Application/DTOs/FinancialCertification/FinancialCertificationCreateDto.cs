namespace WsUtaSystem.Application.DTOs.FinancialCertification
{
    public class FinancialCertificationCreateDto
    {
        public int? RequestId { get; set; }
        public string CertCode { get; set; } = null!;
        public string? CertNumber { get; set; }
        public string? Budget { get; set; }
        public DateTime? CertBudgetDate { get; set; }
        public decimal? RmuHour { get; set; }
        public decimal? RmuCon { get; set; }
        public string? filename { get; set; }
        public string? filepath { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        //public DateTime? UpdatedAt { get; set; }
        //public int? UpdatedBy { get; set; }
        public int? Status { get; set; }
    }
}


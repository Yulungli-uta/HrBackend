namespace WsUtaSystem.Models
{
    public class FinancialCertification
    {
        public int CertificationId { get; set; }
        public int? RequestId { get; set; }
        public string CertCode { get; set; } = null!;
        public string? CertNumber { get; set; }
        public string? Budget { get; set; }
        public DateTime? CertBudgetDate { get; set; }
        public decimal? RmuHour { get; set; }
        public decimal? RmuCon { get; set; }
        public int? Status { get; set; }
    }
}


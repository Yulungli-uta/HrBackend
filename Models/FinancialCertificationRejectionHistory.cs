namespace WsUtaSystem.Models
{
    public class FinancialCertificationRejectionHistory
    {
        public int RejectionHistoryId { get; set; }
        public int CertificationId { get; set; }

        /// <summary>FK → HR.ref_Types, Category=FIN_CERT_REJECTION_TYPE (TEMPORAL | DEFINITIVO).</summary>
        public int? RejectionTypeId { get; set; }

        public string? RejectionReason { get; set; }
        public DateTime RejectedAt { get; set; }
        public int? RejectedBy { get; set; }

        public FinancialCertification? Certification { get; set; }
    }
}

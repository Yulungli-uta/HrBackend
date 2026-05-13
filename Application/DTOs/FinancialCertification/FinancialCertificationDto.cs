namespace WsUtaSystem.Application.DTOs.FinancialCertification
{
    public class FinancialCertificationDto
    {
        public int CertificationId { get; set; }
        public int? RequestId { get; set; }
        public string CertCode { get; set; } = null!;
        public string? CertNumber { get; set; }
        public string? Budget { get; set; }
        public DateTime? CertBudgetDate { get; set; }
        public decimal? RmuHour { get; set; }
        public decimal? RmuCon { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public int? Status { get; set; }

        public string? RejectionReason { get; set; }
        public DateTime? RejectedAt { get; set; }
        public int? RejectedBy { get; set; }
        public int? RejectionTypeId { get; set; }

        /// <summary>Nombre del estado desde ref_Types (FIN_CERT_STATUS).</summary>
        public string? StatusName { get; set; }

        /// <summary>Resumen de la solicitud padre: cuántos se solicitaron y cuántos faltan.</summary>
        public ContractRequestSummary? RequestSummary { get; set; }
    }

    public class ContractRequestSummary
    {
        public int RequestId { get; set; }
        public int NumberOfPeopleToHire { get; set; }
        public int TotalPeopleHired { get; set; }
        public int PendingCount { get; set; }
    }
}

/// <summary>Filtros para consultar certificaciones financieras.</summary>
public sealed record FinancialCertificationQueryFilter(
    string? StatusName,
    int? RequestId,
    string? CertCode,
    string? Search,
    int Page = 1,
    int PageSize = 20
);

/// <summary>Resultado paginado de certificaciones financieras.</summary>
public sealed record PagedFinancialCertificationResult(
    IReadOnlyList<WsUtaSystem.Application.DTOs.FinancialCertification.FinancialCertificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

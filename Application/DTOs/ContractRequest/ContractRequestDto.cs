namespace WsUtaSystem.Application.DTOs.ContractRequest
{
    public class ContractRequestDto
    {
        public int RequestId { get; set; }
        public int? DepartmentId { get; set; }
        public int? WorkModalityId { get; set; }
        public int NumberOfPeopleToHire { get; set; } = 0;
        public decimal NumberHour { get; set; } = 0;
        public int TotalPeopleHired { get; set; } = 0;
        public string? Observation { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

        public int? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? PendingCorrectionReason { get; set; }

        /// <summary>Personas pendientes de contratar (calculado: NumberOfPeopleToHire - TotalPeopleHired).</summary>
        public int PendingCount { get; set; }

        /// <summary>Nombre del estado desde ref_Types (CONTRACT_REQUEST_STATUS).</summary>
        public string? StatusName { get; set; }
    }
}

/// <summary>Filtros para consultar solicitudes de contrato.</summary>
public sealed record ContractRequestQueryFilter(
    string? StatusName,
    int? DepartmentId,
    int? WorkModalityId,
    string? Search,
    int Page = 1,
    int PageSize = 20
);

/// <summary>Resultado paginado de solicitudes de contrato.</summary>
public sealed record PagedContractRequestResult(
    IReadOnlyList<WsUtaSystem.Application.DTOs.ContractRequest.ContractRequestDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);


namespace WsUtaSystem.Application.DTOs.Employees
{
    public class EmployeeCompleteStatsDto
    {
        public long Total { get; set; }
        public long Active { get; set; }
        public long Inactive { get; set; }
        public List<ContractTypeCountDto> ByContractType { get; set; } = new();
    }
}

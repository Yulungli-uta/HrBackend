namespace WsUtaSystem.Application.DTOs.Reports
{
    public sealed class ScheduleContractCountDto
    {
        public int? ScheduleID { get; set; }
        public int? DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
    }
}

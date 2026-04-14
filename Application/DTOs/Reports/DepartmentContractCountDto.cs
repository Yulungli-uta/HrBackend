namespace WsUtaSystem.Application.DTOs.Reports
{
    public sealed class DepartmentContractCountDto
    {
        public int? DepartmentID { get; set; }
        public string Department { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public int TotalEmployees { get; set; }
    }
}

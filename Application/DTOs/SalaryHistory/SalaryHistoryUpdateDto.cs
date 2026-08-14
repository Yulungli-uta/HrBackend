namespace WsUtaSystem.Application.DTOs.SalaryHistory;
public class SalaryHistoryUpdateDto
{
    public int SalaryHistoryId { get; set; }
    public int? ContractId { get; set; }
    public int? ActionId { get; set; }
    public int? EmployeeId { get; set; }
    public decimal OldSalary { get; set; }
    public decimal NewSalary { get; set; }
    public string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; }
}

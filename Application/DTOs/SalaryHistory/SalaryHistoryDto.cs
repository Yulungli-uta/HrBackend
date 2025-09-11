namespace WsUtaSystem.Application.DTOs.SalaryHistory;
public class SalaryHistoryDto
{
    //public class SalaryHistory { get; set; }
    public int SalaryHistoryId { get; set; }
    public int ContractId { get; set; }
    public decimal OldSalary { get; set; }
    public decimal NewSalary { get; set; }
    public string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; }
}

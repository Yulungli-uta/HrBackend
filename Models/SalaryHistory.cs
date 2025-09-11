
namespace WsUtaSystem.Models;
public class SalaryHistory {
  public int SalaryHistoryId { get; set; }
  public int ContractId { get; set; }
  public decimal OldSalary { get; set; }
  public decimal NewSalary { get; set; }
  public string ChangedBy { get; set; } = null!;
  public DateTime ChangedAt { get; set; }
  public string? Reason { get; set; }
}

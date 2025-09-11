
namespace WsUtaSystem.Models;
public class Payroll {
  public int PayrollId { get; set; }
  public int EmployeeId { get; set; }
  public string Period { get; set; } = null!;
  public decimal BaseSalary { get; set; }
  public string Status { get; set; } = null!;
  public DateOnly? PaymentDate { get; set; }
  public string? BankAccount { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime? UpdatedAt { get; set; }
}

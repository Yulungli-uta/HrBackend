namespace WsUtaSystem.Application.DTOs.Payroll;
public class PayrollCreateDto
{
    //public class Payroll { get; set; }
    public int PayrollId { get; set; }
    public int EmployeeId { get; set; }
    public string Period { get; set; }
    public decimal BaseSalary { get; set; }
    public string Status { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string BankAccount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

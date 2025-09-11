namespace WsUtaSystem.Application.DTOs.Overtime;
public class OvertimeCreateDto
{
    //public class Overtime { get; set; }
    public int OvertimeId { get; set; }
    public int EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; }
    public string OvertimeType { get; set; }
    public decimal Hours { get; set; }
    public string Status { get; set; }
    public int ApprovedBy { get; set; }
    public int SecondApprover { get; set; }
    public decimal Factor { get; set; }
    public decimal ActualHours { get; set; }
    public decimal PaymentAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

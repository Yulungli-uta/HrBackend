namespace WsUtaSystem.Application.DTOs.PunchJustifications;
public class PunchJustificationsDto
{
    public int PunchJustID { get; set; }
    //public int? PunchID { get; set; }
    public int EmployeeID { get; set; }
    public int BossEmployeeID { get; set; }
    public int JustificationTypeID { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public DateOnly? JustificationDate { get; set; }
    //public required string Reason { get; set; }
    public string Reason { get; set; }
    public decimal? HoursRequested { get; set; }
    public bool Approved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string? Comments { get; set; }
    //public required string Status { get; set; }
    public string Status { get; set; }
}

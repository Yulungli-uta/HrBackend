namespace WsUtaSystem.Application.DTOs.PunchJustifications;
public class PunchJustificationsDto
{
    public int PunchJustId { get; set; }
    public int EmployeeId { get; set; }
    public int BossEmployeeId { get; set; }
    public int JustificationTypeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? JustificationDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? HoursRequested { get; set; }
    public bool Approved { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string? Comments { get; set; }
    public string Status { get; set; } = "PENDING";
}

namespace WsUtaSystem.Application.DTOs.TimeRecoveryLogs;
public class TimeRecoveryLogsCreateDto
{
    //public class TimeRecoveryLogs { get; set; }
    public int RecoveryLogId { get; set; }
    public int RecoveryPlanId { get; set; }
    public DateOnly ExecutedDate { get; set; }
    public int MinutesRecovered { get; set; }
    public int ApprovedBy { get; set; }
    public DateTime ApprovedAt { get; set; }
}

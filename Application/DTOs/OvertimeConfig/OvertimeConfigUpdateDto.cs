namespace WsUtaSystem.Application.DTOs.OvertimeConfig;
public class OvertimeConfigUpdateDto
{
    //public class OvertimeConfig { get; set; }
    public string OvertimeType { get; set; }
    public decimal Factor { get; set; }
    public string Description { get; set; }
    public int? MaxDailyMinutes { get; set; }
    public int? MaxWeeklyMinutes { get; set; }
}

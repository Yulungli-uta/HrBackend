
namespace WsUtaSystem.Models;
public class OvertimeConfig {
  public string OvertimeType { get; set; } = null!;
  public decimal Factor { get; set; }
  public string? Description { get; set; }
  public int? MaxDailyMinutes { get; set; }
  public int? MaxWeeklyMinutes { get; set; }
}

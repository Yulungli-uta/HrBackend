
namespace WsUtaSystem.Models;
public class JobExecutionLog {
  public long JobLogId { get; set; }
  public string JobName { get; set; } = null!;
  public string Source { get; set; } = null!;
  public DateTime StartedAt { get; set; }
  public DateTime? FinishedAt { get; set; }
  public string Status { get; set; } = null!;
  public string? ErrorMessage { get; set; }
  public int? DurationMs { get; set; }
}

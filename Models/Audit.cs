
namespace WsUtaSystem.Models;
public class Audit {
  public long AuditId { get; set; }
  public string TableName { get; set; } = null!;
  public string Action { get; set; } = null!;
  public string RecordId { get; set; } = null!;
  public string UserName { get; set; } = null!;
  public DateTime DateTime { get; set; }
  public string? Details { get; set; }
}

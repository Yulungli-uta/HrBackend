
namespace WsUtaSystem.Models;
public class PayrollLines {
  public int PayrollLineId { get; set; }
  public int PayrollId { get; set; }
  public string LineType { get; set; } = null!;
  public string Concept { get; set; } = null!;
  public decimal Quantity { get; set; }
  public decimal UnitValue { get; set; }
  public string? Notes { get; set; }
}

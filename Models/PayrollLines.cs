
namespace WsUtaSystem.Models;
public class PayrollLines {
  public int PayrollLineId { get; set; }
  public int PayrollId { get; set; }
  public string LineType { get; set; } = null!;
  public string Concept { get; set; } = null!;
  public decimal Quantity { get; set; }
  public decimal UnitValue { get; set; }
  public string? Notes { get; set; }
  /// <summary>Régimen laboral (ref_Types CONTRACT_TYPE) que originó la línea, cuando aplica. Solo poblado hoy para líneas de horas extra (siempre 57=LOSEP); descuentos/subsidios todavía no separan por régimen.</summary>
  public int? LaborRegimeId { get; set; }
}

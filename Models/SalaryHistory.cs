
namespace WsUtaSystem.Models;
public class SalaryHistory {
  public int SalaryHistoryId { get; set; }
  /// <summary>Documento fuente: contrato (Código de Trabajo). Nulo si el origen fue una acción de personal.</summary>
  public int? ContractId { get; set; }
  /// <summary>Documento fuente: acción de personal económica (LOSEP/LOES). Nulo si el origen fue un contrato.</summary>
  public int? ActionId { get; set; }
  public int? EmployeeId { get; set; }
  public decimal OldSalary { get; set; }
  public decimal NewSalary { get; set; }
  public string ChangedBy { get; set; } = null!;
  public DateTime ChangedAt { get; set; }
  public string? Reason { get; set; }
}

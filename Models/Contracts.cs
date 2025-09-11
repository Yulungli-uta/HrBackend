
namespace WsUtaSystem.Models;
public class Contracts {
  public int ContractId { get; set; }
  public int EmployeeId { get; set; }
  public int ContractType { get; set; } 
    public int? JobId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? DocumentNum { get; set; }           // N�mero de documento (obligatorio)
    public string? Motivation { get; set; }           // Motivaci�n
    public string? BudgetItem { get; set; }           // Partida presupuestaria
    public int? Grade { get; set; }                   // Grado
    public string? GovernanceLevel { get; set; }      // Nivel de gesti�n
    public string? Workplace { get; set; }            // Lugar de trabajo

    public decimal? BaseSalary { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime CreatedAt { get; set; }
  public int? UpdatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
}

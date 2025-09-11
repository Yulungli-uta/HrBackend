
namespace WsUtaSystem.Models;
public class Contracts {
  public int ContractId { get; set; }
  public int EmployeeId { get; set; }
  public int ContractType { get; set; } 
    public int? JobId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? DocumentNum { get; set; }           // Número de documento (obligatorio)
    public string? Motivation { get; set; }           // Motivación
    public string? BudgetItem { get; set; }           // Partida presupuestaria
    public int? Grade { get; set; }                   // Grado
    public string? GovernanceLevel { get; set; }      // Nivel de gestión
    public string? Workplace { get; set; }            // Lugar de trabajo

    public decimal? BaseSalary { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime CreatedAt { get; set; }
  public int? UpdatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
}

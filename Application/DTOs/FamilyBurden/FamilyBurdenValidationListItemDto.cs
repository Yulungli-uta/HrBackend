namespace WsUtaSystem.Application.DTOs.FamilyBurden;

/// <summary>
/// Fila de la pantalla de validación de cargas familiares: incluye el nombre del
/// empleado titular y los nombres resueltos de los catálogos (estado, discapacidad),
/// ya que el listado simple de <see cref="FamilyBurdenDto"/> no los trae.
/// </summary>
public class FamilyBurdenValidationListItemDto
{
    public int BurdenId { get; set; }
    public int PersonId { get; set; }
    public string EmployeeFullName { get; set; } = string.Empty;
    public string EmployeeIdCard { get; set; } = string.Empty;
    public string DependentId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public int? DisabilityTypeId { get; set; }
    public string? DisabilityTypeName { get; set; }
    public int? StatusTypeId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectedByName { get; set; }
    public string? RejectionReason { get; set; }
}

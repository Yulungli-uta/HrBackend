namespace WsUtaSystem.Models.Views;

/// <summary>
/// Mapea HR.vw_SiiesFuncionarios: un renglón por empleado con los datos crudos/homologados
/// necesarios para construir el reporte SIIES Funcionarios (matrices 5.7/5.8). Vista de solo
/// lectura, sin clave primaria. Las reglas condicionales SIIES se resuelven en
/// SiiesFuncionariosReportSource, no en este modelo.
/// </summary>
public class VwSiiesFuncionario
{
    public int EmployeeID { get; set; }
    public int PersonID { get; set; }
    public int? IdentType { get; set; }
    public string? IdentTypeName { get; set; }
    public string IDCard { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly? BirthDate { get; set; }
    public string? SexSiiesLabel { get; set; }
    public string? GenderSiiesLabel { get; set; }
    public string? CountryId { get; set; }
    public string? EthnicityName { get; set; }
    public string? EthnicitySiiesLabel { get; set; }
    public string? IndigenousNationalitySiiesLabel { get; set; }
    public string? DisabilitySiiesLabel { get; set; }
    public decimal? DisabilityPercentage { get; set; }
    public string? CONADISCard { get; set; }
    public string? InstitutionalEmail { get; set; }
    public string? DepartmentName { get; set; }
    public string? JobDescription { get; set; }
    public bool PuestoJerarquicoSuperior { get; set; }
    public string? TipoFuncionarioSiiesLabel { get; set; }
    public string? TipoDocenteLoesSiiesLabel { get; set; }
    public string? CategoriaDocenteLoesSiiesLabel { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool? RegimeIsActive { get; set; }
    public bool? IngresoPorConcurso { get; set; }
    public string? RegimeDocumentType { get; set; }
    public string? RelacionIesSiiesLabel { get; set; }
    public decimal? ContractedHours { get; set; }
    public bool EmployeeIsActive { get; set; }
    public DateOnly HireDate { get; set; }
}

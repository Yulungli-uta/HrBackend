using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Application.DTOs.DepartmentAuthority;

/// <summary>
/// DTO para la creación de una nueva autoridad de departamento.
/// Incluye validaciones de datos de entrada siguiendo los principios SOLID (SRP).
/// </summary>
public class DepartmentAuthorityCreateDto
{
    /// <summary>ID del departamento al que se asigna la autoridad. Requerido.</summary>
    [Required(ErrorMessage = "El departamento es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El departamento debe ser un ID válido.")]
    public int DepartmentId { get; set; }

    /// <summary>ID del empleado designado como autoridad. Requerido.</summary>
    [Required(ErrorMessage = "El empleado es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El empleado debe ser un ID válido.")]
    public int EmployeeId { get; set; }

    /// <summary>ID del tipo de autoridad (FK a HR.ref_Types). Requerido.</summary>
    [Required(ErrorMessage = "El tipo de autoridad es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El tipo de autoridad debe ser un ID válido.")]
    public int AuthorityTypeId { get; set; }

    /// <summary>ID del cargo asociado (opcional).</summary>
    public int? JobId { get; set; }

    /// <summary>Denominación oficial del nombramiento (máx. 200 caracteres).</summary>
    [MaxLength(200, ErrorMessage = "La denominación no puede superar los 200 caracteres.")]
    public string? Denomination { get; set; }

    /// <summary>Fecha de inicio de la vigencia. Requerida.</summary>
    [Required(ErrorMessage = "La fecha de inicio es requerida.")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    /// <summary>Fecha de fin de la vigencia (opcional). Debe ser posterior a StartDate.</summary>
    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    /// <summary>Código o número de resolución de respaldo (máx. 100 caracteres).</summary>
    [MaxLength(100, ErrorMessage = "El código de resolución no puede superar los 100 caracteres.")]
    public string? ResolutionCode { get; set; }

    /// <summary>Observaciones adicionales (máx. 500 caracteres).</summary>
    [MaxLength(500, ErrorMessage = "Las notas no pueden superar los 500 caracteres.")]
    public string? Notes { get; set; }

    /// <summary>Indica si el registro está activo. Por defecto: true.</summary>
    public bool IsActive { get; set; } = true;
}

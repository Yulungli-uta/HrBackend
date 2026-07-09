using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>
/// Solicitud interna genérica del empleado autenticado: corrección de datos, documentos,
/// información u otros trámites administrativos que no tienen tabla propia.
/// </summary>
public sealed class EmployeeInternalRequest : IAuditable
{
    public int RequestId { get; set; }

    public int EmployeeId { get; set; }

    /// <summary>ACTUALIZACION_DATOS, DOCUMENTO, INFORMACION, OTRO.</summary>
    public string RequestType { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>PENDIENTE, EN_REVISION, DEVUELTO, APROBADO, RECHAZADO, ANULADO, COMPLETADO.</summary>
    public string Status { get; set; } = "PENDIENTE";

    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }
    public int? ResolvedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? CancelledBy { get; set; }

    public byte[]? RowVersion { get; set; }

    public Employees? Employee { get; set; }
    public ICollection<EmployeeInternalRequestStatusHistory> StatusHistory { get; set; } = [];
}

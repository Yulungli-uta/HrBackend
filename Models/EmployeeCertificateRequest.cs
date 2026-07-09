using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>Solicitud de certificado laboral del empleado autenticado.</summary>
public sealed class EmployeeCertificateRequest : IAuditable
{
    public int RequestId { get; set; }

    public int EmployeeId { get; set; }

    /// <summary>LABORAL, INGRESOS, ANTIGUEDAD, ... (constantes en EmployeeCertificateType).</summary>
    public string CertificateType { get; set; } = "LABORAL";

    public string? Purpose { get; set; }

    /// <summary>PENDIENTE, EMITIDO, RECHAZADO, ANULADO.</summary>
    public string Status { get; set; } = "PENDIENTE";

    /// <summary>FK al documento PDF generado (HR.tbl_GeneratedDocuments), poblado al emitir.</summary>
    public int? GeneratedDocumentId { get; set; }

    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public DateTime? IssuedAt { get; set; }
    public int? IssuedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? RejectedBy { get; set; }

    public byte[]? RowVersion { get; set; }

    public Employees? Employee { get; set; }
    public ICollection<EmployeeCertificateStatusHistory> StatusHistory { get; set; } = [];
}

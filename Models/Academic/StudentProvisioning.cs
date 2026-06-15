namespace WsUtaSystem.Models.Academic;

/// <summary>
/// Seguimiento del aprovisionamiento AD de un estudiante.
/// RepositoryUta crea/deshabilita la cuenta; HrBackend persiste el estado aquí.
/// </summary>
public class StudentProvisioning
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK → tbl_Students.StudentId (mismo DB).</summary>
    public int StudentId { get; set; }

    /// <summary>Email institucional generado por RepositoryUta (ej: jsmith@uta.edu.ec).</summary>
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string? GivenName { get; set; }
    public string? Surname { get; set; }

    public int ProvisioningStatusId { get; set; } = (int)StudentProvisioningStatus.Requested;
    public string? ProvisioningStatusName { get; set; } = nameof(StudentProvisioningStatus.Requested);

    /// <summary>DN o GUID retornado por RepositoryUta tras crear la cuenta AD.</summary>
    public string? AdObjectId { get; set; }

    /// <summary>Referencia al período académico que disparó el aprovisionamiento.</summary>
    public string? SourceReference { get; set; }

    public string? ErrorMessage { get; set; }
    public string? RequestedBy { get; set; }

    public DateTime? ProvisionedAt { get; set; }
    public DateTime? DisabledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Students? Student { get; set; }
}

public enum StudentProvisioningStatus
{
    Requested   = 3001,
    CreatedInAd = 3002,
    AdFailed    = 3003,
    Disabled    = 3004,
}

namespace WsUtaSystem.Application.DTOs.Publications;
public class PublicationsDto
{
    //public class Publications { get; set; }
    public int PublicationId { get; set; }
    public int PersonId { get; set; }
    public string Location { get; set; }
    public int PublicationTypeId { get; set; }
    public bool IsIndexed { get; set; }
    public int JournalTypeId { get; set; }
    public string Issn_Isbn { get; set; }
    public string JournalName { get; set; }
    public string JournalNumber { get; set; }
    public string Volume { get; set; }
    public string Pages { get; set; }
    public string? Doi { get; set; }
    public string? Link { get; set; }
    public int KnowledgeAreaTypeId { get; set; }
    public int SubAreaTypeId { get; set; }
    public int AreaTypeId { get; set; }
    public string Title { get; set; }
    public string OrganizedBy { get; set; }
    public string EventName { get; set; }
    public string EventEdition { get; set; }
    public DateOnly PublicationDate { get; set; }
    public bool UTAffiliation { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Origen del registro (ref_Types SCIENTIFIC_PROVIDER): MANUAL, MIGRACION o un proveedor como SCOPUS.</summary>
    public int SourceProviderTypeId { get; set; }
    public string? ExternalId { get; set; }
    /// <summary>SIN_REVISION, APROBADA (congelada) o RECHAZADA.</summary>
    public string ApprovalStatus { get; set; } = "SIN_REVISION";
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>La publicación no se puede modificar ni eliminar (aprobada o rechazada).</summary>
    public bool IsLocked { get; set; }
    /// <summary>El origen exige revisión de la Dirección de Investigación (lo sincronizado de un proveedor).</summary>
    public bool RequiresApproval { get; set; }
    /// <summary>Cuenta en el CV en PDF y en la promoción docente.</summary>
    public bool CountsForCv { get; set; }
}

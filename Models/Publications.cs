
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class Publications : IAuditable 
{
  public int PublicationId{get;set;}
  public int PersonId{get;set;}
  public string? Location{get;set;}
  public int? PublicationTypeId{get;set;}
  public bool? IsIndexed{get;set;}
  public int? JournalTypeId{get;set;}
  public string? Issn_Isbn{get;set;}
  public string? JournalName{get;set;}
  public string? JournalNumber{get;set;}
  public string? Volume{get;set;}
  public string? Pages{get;set;}
  public string? Doi{get;set;}
  public string? Link{get;set;}
  public int? KnowledgeAreaTypeId{get;set;}
  public int? SubAreaTypeId{get;set;}
  public int? AreaTypeId{get;set;}
  public string Title{get;set;}=null!;
  public string? OrganizedBy{get;set;}
  public string? EventName{get;set;}
  public string? EventEdition{get;set;}
  public DateOnly? PublicationDate{get;set;}
  public bool? UTAffiliation{get;set;}

  /// <summary>Origen del registro (ref_Types, categoría SCIENTIFIC_PROVIDER): MANUAL, MIGRACION o un proveedor como SCOPUS.</summary>
  public int SourceProviderTypeId{get;set;}
  /// <summary>ID del artículo en el proveedor (p. ej. Scopus ID). Junto con PersonId y el proveedor evita duplicados al sincronizar.</summary>
  public string? ExternalId{get;set;}
  /// <summary>DOI normalizado (columna calculada de solo lectura).</summary>
  public string? DoiNormalized{get;set;}
  /// <summary>SIN_REVISION, APROBADA (congelada) o RECHAZADA. Ver <see cref="WsUtaSystem.Application.Common.PublicationApprovalStatus"/>.</summary>
  public string ApprovalStatus{get;set;}=WsUtaSystem.Application.Common.PublicationApprovalStatus.SinRevision;
  public DateTime? ApprovedAt{get;set;}
  public int? ApprovedBy{get;set;}
  public string? ReviewNotes{get;set;}
  public DateTime? LastSyncedAt{get;set;}
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}


using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class Trainings : IAuditable 
{
  public int TrainingId{get;set;}
  public int PersonId{get;set;}
  public string? Location{get;set;}
  public string Title{get;set;}=null!;
  public string Institution{get;set;}=null!;
  public int? KnowledgeAreaTypeId{get;set;}
  public int? EventTypeId{get;set;}
  public string? CertifiedBy{get;set;}
  public int? CertificateTypeId{get;set;}
  public DateOnly StartDate{get;set;}
  public DateOnly EndDate{get;set;}
  public int Hours{get;set;}
  public int? ApprovalTypeId{get;set;}
  public int? TrainingDirectionTypeId{get;set;}
  public int? ModalityTypeId{get;set;}
  public string? CountryId{get;set;}
  /// <summary>Pedagógica (true) o Específica (false) -- null = sin definir (registro viejo).</summary>
  public bool? IsPedagogical{get;set;}
  /// <summary>Origen del registro: Manual (digitado), DEaDV (sincronizado) o Importado (carga masiva histórica).</summary>
  public string Source{get;set;}="Manual";
  /// <summary>Identificador en el sistema de origen (cert_code de DEaDV); clave anti-duplicado al sincronizar.</summary>
  public string? ExternalId{get;set;}
  /// <summary>URL del certificado en DEaDV. Nunca se expone al navegador: se sirve por proxy del backend.</summary>
  public string? CertificateUrl{get;set;}
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
}

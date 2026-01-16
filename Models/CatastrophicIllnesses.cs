
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class CatastrophicIllnesses : IAuditable{
  public int IllnessId{get;set;}
  public int PersonId{get;set;}
  public string Illness{get;set;}=null!;
  public string? IESSNumber{get;set;}
  public string? SubstituteName{get;set;}
  public int IllnessTypeId{get;set;}
  public string? CertificateNumber{get;set;}
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
}


namespace WsUtaSystem.Models;
public class CatastrophicIllnesses {
  public int IllnessId{get;set;}
  public int PersonId{get;set;}
  public string Illness{get;set;}=null!;
  public string? IESSNumber{get;set;}
  public string? SubstituteName{get;set;}
  public int IllnessTypeId{get;set;}
  public string? CertificateNumber{get;set;}
  public DateTime CreatedAt{get;set;}
}

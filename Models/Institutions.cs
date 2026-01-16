
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class Institutions : IAuditable{
  public int InstitutionId{get;set;}
  public string Name{get;set;}=null!;
  public int InstitutionTypeId{get;set;}
  public string CountryId{get;set;}
  public string ProvinceId{get;set;}
  public string CantonId{get;set;}
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
}

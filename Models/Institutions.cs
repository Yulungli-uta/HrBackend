
namespace WsUtaSystem.Models;
public class Institutions {
  public int InstitutionId{get;set;}
  public string Name{get;set;}=null!;
  public int InstitutionTypeId{get;set;}
  public string CountryId{get;set;}
  public string ProvinceId{get;set;}
  public string CantonId{get;set;}
  public DateTime CreatedAt{get;set;}
}

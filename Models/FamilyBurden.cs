
namespace WsUtaSystem.Models;
public class FamilyBurden {
  public int BurdenId{get;set;}
  public int PersonId{get;set;}
  public string DependentId{get;set;}=null!;
  public int IdentificationTypeId{get;set;}
  public string FirstName{get;set;}=null!;
  public string LastName{get;set;}=null!;
  public DateOnly BirthDate{get;set;}
  public int? DisabilityTypeId{get;set;}
  public DateTime CreatedAt{get;set;}
}

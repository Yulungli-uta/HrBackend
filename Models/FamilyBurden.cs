
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class FamilyBurden : IAuditable{
  public int BurdenId{get;set;}
  public int PersonId{get;set;}
  public string DependentId{get;set;}=null!;
  public int IdentificationTypeId{get;set;}
  public string FirstName{get;set;}=null!;
  public string LastName{get;set;}=null!;
  public DateOnly BirthDate{get;set;}
  public int? DisabilityTypeId{get;set;}
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
}

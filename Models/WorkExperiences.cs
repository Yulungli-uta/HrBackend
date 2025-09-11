
namespace WsUtaSystem.Models;
public class WorkExperiences {
  public int WorkExpId{get;set;}
  public int PersonId{get;set;}
  public string? CountryId{get;set;}
  public string Company{get;set;}=null!;
  public int? InstitutionTypeId{get;set;}
  public string? EntryReason{get;set;}
  public string? ExitReason{get;set;}
  public string Position{get;set;}=null!;
  public string? InstitutionAddress{get;set;}
  public DateOnly StartDate{get;set;}
  public DateOnly? EndDate{get;set;}
  public int? ExperienceTypeId{get;set;}
  public bool IsCurrent{get;set;}
  public DateTime CreatedAt{get;set;}
}

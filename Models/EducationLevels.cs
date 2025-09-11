
namespace WsUtaSystem.Models;
public class EducationLevels {
  public int EducationId{get;set;}
  public int PersonId{get;set;}
  public int EducationLevelTypeId{get;set;}
  public int InstitutionId{get;set;}
  public string Title{get;set;}=null!;
  public string? Specialty{get;set;}
  public DateOnly? StartDate{get;set;}
  public DateOnly? EndDate{get;set;}
  public string? Grade{get;set;}
  public string? Location{get;set;}
  public decimal? Score{get;set;}
  public DateTime CreatedAt{get;set;}
}

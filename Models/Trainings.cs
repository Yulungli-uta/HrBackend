
namespace WsUtaSystem.Models;
public class Trainings {
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
  public DateTime CreatedAt{get;set;}
}

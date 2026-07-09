
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class Languages : IAuditable
{
  public int LanguageId{get;set;}
  public int PersonId{get;set;}
  public int LanguageTypeId{get;set;}
  public int LevelTypeId{get;set;}
  public string? ReferenceFramework{get;set;}
  public string? CertifyingInstitution{get;set;}
  public string? CountryId{get;set;}
  public DateOnly IssueDate{get;set;}
  public DateOnly? ExpirationDate{get;set;}
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
}

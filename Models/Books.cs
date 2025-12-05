
namespace WsUtaSystem.Models;
public class Books {
  public int BookId{get;set;}
  public int PersonId{get;set;}
  public string Title{get;set;}=null!;
  public bool? PeerReviewed{get;set;}
  public string? ISBN{get;set;}
  public string? Publisher{get;set;}
  public string? CountryId{get;set;}
  public string? City{get;set;}
  public int? KnowledgeAreaTypeId{get;set;}
  public int? SubAreaTypeId{get;set;}
  public int? AreaTypeId{get;set;}
  public int? VolumeCount{get;set;}
  public int? ParticipationTypeId{get;set;}
  public DateOnly? PublicationDate{get;set;}
  public bool? UTAffiliation{get;set;}
  public bool? UTASponsorship{get;set;}
  public int? BookTypeId { get; set; }
  public DateTime CreatedAt{get;set;}
}

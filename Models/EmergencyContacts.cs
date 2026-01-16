
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;
public class EmergencyContacts : IAuditable{
  public int ContactId{get;set;}
  public int PersonId{get;set;}
  public string Identification{get;set;}=null!;
  public string FirstName{get;set;}=null!;
  public string LastName{get;set;}=null!;
  public int RelationshipTypeId{get;set;}
  public string? Address{get;set;}
  public string? Phone{get;set;}
  public string? Mobile{get;set;}
  public DateTime? CreatedAt { get; set; }
  public int? CreatedBy { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public int? UpdatedBy { get; set; }
}

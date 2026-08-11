namespace WsUtaSystem.Application.DTOs.EmergencyContacts;
public class EmergencyContactsUpdateDto
{
    //public class EmergencyContacts { get; set; }
    public int ContactId { get; set; }
    public int PersonId { get; set; }
    public string Identification { get; set; } = null!;
    public int? IdentificationTypeId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int RelationshipTypeId { get; set; }
    public string? Address { get; set; }
    public string Phone { get; set; } = null!;
    public string? Mobile { get; set; }
    public DateTime CreatedAt { get; set; }
}

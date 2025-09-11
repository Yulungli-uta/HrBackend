namespace WsUtaSystem.Application.DTOs.EmergencyContacts;
public class EmergencyContactsUpdateDto
{
    //public class EmergencyContacts { get; set; }
    public int ContactId { get; set; }
    public int PersonId { get; set; }
    public string Identification { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int RelationshipTypeId { get; set; }
    public string Address { get; set; }
    public string Phone { get; set; }
    public string Mobile { get; set; }
    public DateTime CreatedAt { get; set; }
}

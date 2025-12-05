namespace WsUtaSystem.Application.DTOs.FamilyBurden;
public class FamilyBurdenUpdateDto
{
    //public class FamilyBurden { get; set; }
    public int BurdenId { get; set; }
    public int PersonId { get; set; }
    public string DependentId { get; set; }
    public int IdentificationTypeId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public int? DisabilityTypeId { get; set; }
    public DateTime? CreatedAt { get; set; }
}

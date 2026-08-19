namespace WsUtaSystem.Application.DTOs.FamilyBurden;
public class FamilyBurdenDto
{
    ////public class FamilyBurden { get; set; }
    public int BurdenId { get; set; }
    public int PersonId { get; set; }
    public string? DependentId { get; set; }
    public int IdentificationTypeId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateOnly BirthDate { get; set; }
    public int? DisabilityTypeId { get; set; }
    public decimal? DisabilityPercentage { get; set; }
    public int? StatusTypeId { get; set; }
    public string? StatusName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public int? ApprovedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public int? RejectedBy { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

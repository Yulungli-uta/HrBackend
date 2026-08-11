using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Application.DTOs.FamilyBurden;

/// <summary>DTO multipart para crear una carga familiar junto con su documento de respaldo.</summary>
public class FamilyBurdenWithDocumentCreateDto
{
    public int PersonId { get; set; }
    public string DependentId { get; set; } = null!;
    public int IdentificationTypeId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly BirthDate { get; set; }
    public int? DisabilityTypeId { get; set; }

    public IFormFile? File { get; set; }
    public int? DocumentTypeId { get; set; }
}

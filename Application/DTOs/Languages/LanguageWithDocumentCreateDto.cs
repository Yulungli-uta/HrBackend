using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Application.DTOs.Languages;

/// <summary>DTO multipart para crear una certificación de idioma junto con su certificado de respaldo.</summary>
public class LanguageWithDocumentCreateDto
{
    public int PersonId { get; set; }
    public int LanguageTypeId { get; set; }
    public int LevelTypeId { get; set; }
    public string? ReferenceFramework { get; set; }
    public string? CertifyingInstitution { get; set; }
    public string? CountryId { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }

    public IFormFile? File { get; set; }
    public int? DocumentTypeId { get; set; }
}

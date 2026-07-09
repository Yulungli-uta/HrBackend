namespace WsUtaSystem.Application.DTOs.Languages;
public class LanguagesUpdateDto
{
    public int LanguageId { get; set; }
    public int PersonId { get; set; }
    public int LanguageTypeId { get; set; }
    public int LevelTypeId { get; set; }
    public string? ReferenceFramework { get; set; }
    public string? CertifyingInstitution { get; set; }
    public string? CountryId { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }
}

using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Application.DTOs.EducationLevels;

/// <summary>
/// DTO multipart para crear un registro de Formación Académica junto con su documento
/// de respaldo (título/certificado) en una sola llamada, con garantía transaccional
/// entre el INSERT del registro y el INSERT de la metadata del archivo.
/// </summary>
public class EducationLevelWithDocumentCreateDto
{
    public int PersonId { get; set; }
    public int EducationLevelTypeId { get; set; }
    public int InstitutionId { get; set; }
    public string Title { get; set; } = null!;
    public string? Specialty { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Grade { get; set; }
    public string? Location { get; set; }
    public decimal? Score { get; set; }
    public string? SenescytRegistrationNumber { get; set; }

    /// <summary>Archivo opcional (título/certificado). Si se omite, se crea el registro sin adjunto.</summary>
    public IFormFile? File { get; set; }
    public int? DocumentTypeId { get; set; }
}

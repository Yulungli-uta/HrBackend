using Microsoft.AspNetCore.Http;

namespace WsUtaSystem.Application.DTOs.Trainings;

/// <summary>DTO multipart para crear una capacitación junto con su certificado de respaldo.</summary>
public class TrainingWithDocumentCreateDto
{
    public int PersonId { get; set; }
    public string? Location { get; set; }
    public string Title { get; set; } = null!;
    public string Institution { get; set; } = null!;
    public int? KnowledgeAreaTypeId { get; set; }
    public int EventTypeId { get; set; }
    public string? CertifiedBy { get; set; }
    public int? CertificateTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Hours { get; set; }
    public int? ApprovalTypeId { get; set; }

    public IFormFile? File { get; set; }
    public int? DocumentTypeId { get; set; }
}

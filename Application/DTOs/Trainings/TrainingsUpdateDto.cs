namespace WsUtaSystem.Application.DTOs.Trainings;
public class TrainingsUpdateDto
{
    //public class Trainings { get; set; }
    public int TrainingId { get; set; }
    public int PersonId { get; set; }
    public string? Location { get; set; }
    public string Title { get; set; } = null!;
    public string Institution { get; set; } = null!;
    // Nullable: mismo motivo que en TrainingsCreateDto.cs (coincide con Models/Trainings.cs
    // y con la columna real en BD, que permite NULL para los 3).
    public int? KnowledgeAreaTypeId { get; set; }
    public int EventTypeId { get; set; }
    public string? CertifiedBy { get; set; }
    public int? CertificateTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Hours { get; set; }
    public int? ApprovalTypeId { get; set; }
    public DateTime CreatedAt { get; set; }
    // 2026-10-08: mismo motivo que TrainingsCreateDto.cs.
    public bool? IsPedagogical { get; set; }
    public int? TrainingDirectionTypeId { get; set; }
    public int? ModalityTypeId { get; set; }
    public string? CountryId { get; set; }
}

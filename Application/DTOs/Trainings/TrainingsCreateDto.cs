namespace WsUtaSystem.Application.DTOs.Trainings;
public class TrainingsCreateDto
{
    //public class Trainings { get; set; }
    public int TrainingId { get; set; }
    public int PersonId { get; set; }
    public string? Location { get; set; }
    public string Title { get; set; } = null!;
    public string Institution { get; set; } = null!;
    // Nullable: asi es el modelo real (Models/Trainings.cs) y la columna en BD
    // (HR.tbl_Trainings, IS_NULLABLE=YES para los 3). El frontend ya omite estas claves
    // cuando quedan sin seleccionar (opcionales en el formulario) — con el DTO como "int"
    // no-nullable, la clave ausente se rellenaba con el 0 por defecto de C# y violaba la FK
    // contra el catalogo (ningun tipo tiene Id 0), dando 400 crudo (hallazgo real 2026-10-02).
    public int? KnowledgeAreaTypeId { get; set; }
    public int EventTypeId { get; set; }
    public string? CertifiedBy { get; set; }
    public int? CertificateTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Hours { get; set; }
    public int? ApprovalTypeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

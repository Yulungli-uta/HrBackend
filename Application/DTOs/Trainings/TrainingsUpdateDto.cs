namespace WsUtaSystem.Application.DTOs.Trainings;
public class TrainingsUpdateDto
{
    //public class Trainings { get; set; }
    public int TrainingId { get; set; }
    public int PersonId { get; set; }
    public string Location { get; set; }
    public string Title { get; set; }
    public string Institution { get; set; }
    public int KnowledgeAreaTypeId { get; set; }
    public int EventTypeId { get; set; }
    public string CertifiedBy { get; set; }
    public int CertificateTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Hours { get; set; }
    public int ApprovalTypeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

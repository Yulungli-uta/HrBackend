namespace WsUtaSystem.Application.DTOs.Publications;
public class PublicationsUpdateDto
{
    //public class Publications { get; set; }
    public int PublicationId { get; set; }
    public int PersonId { get; set; }
    public string Location { get; set; }
    public int PublicationTypeId { get; set; }
    public bool IsIndexed { get; set; }
    public int JournalTypeId { get; set; }
    public string Issn_Isbn { get; set; }
    public string JournalName { get; set; }
    public string JournalNumber { get; set; }
    public string Volume { get; set; }
    public string Pages { get; set; }
    public int KnowledgeAreaTypeId { get; set; }
    public int SubAreaTypeId { get; set; }
    public int AreaTypeId { get; set; }
    public string Title { get; set; }
    public string OrganizedBy { get; set; }
    public string EventName { get; set; }
    public string EventEdition { get; set; }
    public DateOnly PublicationDate { get; set; }
    public bool UTAffiliation { get; set; }
    public DateTime? CreatedAt { get; set; }
}

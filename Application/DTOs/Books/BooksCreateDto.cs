namespace WsUtaSystem.Application.DTOs.Books;
public class BooksCreateDto
{
    //public class Books { get; set; }
    public int BookId { get; set; }
    public int PersonId { get; set; }
    public string Title { get; set; }
    public bool PeerReviewed { get; set; }
    public string ISBN { get; set; }
    public string Publisher { get; set; }
    public int CountryId { get; set; }
    public string City { get; set; }
    public int KnowledgeAreaTypeId { get; set; }
    public int SubAreaTypeId { get; set; }
    public int AreaTypeId { get; set; }
    public int VolumeCount { get; set; }
    public int ParticipationTypeId { get; set; }
    public DateOnly PublicationDate { get; set; }
    public bool UTAffiliation { get; set; }
    public bool UTASponsorship { get; set; }
    public DateTime CreatedAt { get; set; }
}

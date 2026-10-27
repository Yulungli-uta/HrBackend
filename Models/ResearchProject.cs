using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

/// <summary>Proyecto de investigación con sus resoluciones (cada una con archivo cargado en el sistema).</summary>
public class ResearchProject : IAuditable
{
    public int ResearchProjectId { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ApprovalResolutionNumber { get; set; } = null!;
    public int? ApprovalFileId { get; set; }
    public bool HasExtension { get; set; }
    public string? ExtensionResolutionNumber { get; set; }
    public DateOnly? ExtensionEndDate { get; set; }
    public int? ExtensionFileId { get; set; }
    public string CompletionResolutionNumber { get; set; } = null!;
    public int? CompletionFileId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public ICollection<ResearchProjectParticipation> Participations { get; set; } = new List<ResearchProjectParticipation>();
}

/// <summary>Participación de una persona (interna del sistema o externa) en un proyecto de investigación.</summary>
public class ResearchProjectParticipation : IAuditable
{
    public int ParticipationId { get; set; }
    public int ResearchProjectId { get; set; }
    public bool IsExternal { get; set; }
    public int? PersonId { get; set; }
    public string? ExternalFullName { get; set; }
    public string? ExternalIdentification { get; set; }
    public string? ExternalInstitution { get; set; }
    public int RoleTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public ResearchProject? ResearchProject { get; set; }
}

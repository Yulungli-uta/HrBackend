namespace WsUtaSystem.Application.DTOs.ResearchProjects;

/// <summary>Tiempo calculado por el sistema entre dos fechas (ambas inclusive).</summary>
public sealed record DurationDto(int Years, int Months, int Days, int TotalDays, string Text);

/// <summary>Resolución con su archivo cargado en el sistema (la URL la genera el sistema).</summary>
public sealed record ResearchResolutionDto(string Number, string? DocumentUrl, string? FileName);

public sealed record ResearchExtensionDto(
    string ResolutionNumber,
    DateOnly EndDate,
    DurationDto Duration,
    string? DocumentUrl,
    string? FileName);

public sealed record ResearchParticipationDto(
    int Id,
    string Code,
    bool IsExternal,
    int? PersonId,
    string FullName,
    string? Identification,
    string? Institution,
    int RoleTypeId,
    string RoleCode,
    string RoleName,
    DateOnly StartDate,
    DateOnly EndDate,
    DurationDto Duration);

public sealed record ResearchProjectDto(
    int Id,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    DurationDto ExecutionTime,
    ResearchResolutionDto ApprovalResolution,
    bool HasExtension,
    ResearchExtensionDto? Extension,
    ResearchResolutionDto CompletionResolution,
    IReadOnlyList<ResearchParticipationDto> Participations);

public sealed record ResearchProjectListItemDto(
    int Id,
    string Code,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    bool HasExtension,
    int ParticipantsCount);

public sealed record ResearchPersonLookupDto(int PersonId, string IdCard, string FullName);

/// <summary>Datos de la participación recibidos del formulario (JSON dentro del multipart).</summary>
public sealed class ResearchParticipationUpsertDto
{
    /// <summary>Id existente al editar; vacío para una participación nueva.</summary>
    public int? Id { get; set; }
    public bool IsExternal { get; set; }
    public int? PersonId { get; set; }
    public string? ExternalFullName { get; set; }
    public string? ExternalIdentification { get; set; }
    public string? ExternalInstitution { get; set; }
    public int RoleTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}

/// <summary>Formulario multipart: datos en JSON + los tres archivos de resolución (PDF).</summary>
public sealed class ResearchProjectFormDto
{
    /// <summary>JSON de <see cref="ResearchProjectUpsertDto"/>.</summary>
    public string? Data { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? ApprovalFile { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? ExtensionFile { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? CompletionFile { get; set; }
}

/// <summary>Datos del proyecto recibidos del formulario (JSON dentro del multipart).</summary>
public sealed class ResearchProjectUpsertDto
{
    public string Name { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string ApprovalResolutionNumber { get; set; } = "";
    public bool HasExtension { get; set; }
    public string? ExtensionResolutionNumber { get; set; }
    public DateOnly? ExtensionEndDate { get; set; }
    public string CompletionResolutionNumber { get; set; } = "";
    public List<ResearchParticipationUpsertDto> Participations { get; set; } = new();
}

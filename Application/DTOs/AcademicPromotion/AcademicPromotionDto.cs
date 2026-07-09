namespace WsUtaSystem.Application.DTOs.AcademicPromotion;

// ── Constantes ───────────────────────────────────────────────────────────────────

public static class TrainingDirection
{
    public const string Received = "RECEIVED_TRAINING";
    public const string Given = "GIVEN_TRAINING";
}

// ── Respuesta ────────────────────────────────────────────────────────────────────

/// <summary>
/// Perfil académico completo de un docente, consumido por procesos de validación,
/// promoción o evaluación académica. Se arma en modo mock (datos sintéticos) o modo
/// base de datos (consulta a los repositorios existentes), según
/// <see cref="Common.Options.AcademicPromotionOptions.UseMockData"/>.
/// </summary>
public sealed record TeacherAcademicProfileDto(
    string TeacherId,
    string IdentificationType,
    string Identification,
    string FullName,
    string? Orcid,
    DependencyDto Dependency,
    string? EmploymentRelationship,
    string? CurrentPosition,
    DateOnly? CurrentPositionStartDate,
    DateOnly EvaluationDate,
    IReadOnlyList<TeacherExperienceDto> Experience,
    IReadOnlyList<TeacherPublicationDto> Publications,
    IReadOnlyList<TeacherTrainingDto> ReceivedTrainings,
    IReadOnlyList<TeacherTrainingDto> GivenTrainings,
    IReadOnlyList<TeacherResearchProjectDto> ResearchProjects,
    IReadOnlyList<TeacherDoctoralThesisDto> DoctoralTheses,
    IReadOnlyList<TeacherLanguageDto> Languages,
    TeacherScoreDto? Score
);

public sealed record DependencyDto(
    string Id,
    string Name
);

public sealed record TeacherExperienceDto(
    string Id,
    string Type,
    string Institution,
    string? Position,
    string? Category,
    DateOnly StartDate,
    DateOnly? EndDate,
    int Years,
    int Months,
    string? KnowledgeArea,
    string? Country,
    string? SupportingDocumentUrl
);

public sealed record TeacherPublicationDto(
    string Id,
    string Type,
    string Name,
    string? Journal,
    string? KnowledgeArea,
    DateOnly PublicationDate,
    string? Doi,
    string? Link,
    string? Language,
    string? IndexingDatabase,
    string? Status,
    string? Country,
    string? SupportingDocumentUrl
);

/// <summary>Se reutiliza para receivedTrainings y givenTrainings; el discriminante es <see cref="Type"/>.</summary>
public sealed record TeacherTrainingDto(
    string Id,
    string Type,
    string? TrainingCategory,
    string Name,
    string Institution,
    DateOnly StartDate,
    DateOnly EndDate,
    int Hours,
    string? KnowledgeArea,
    string? Modality,
    string? Country,
    string? SupportingDocumentUrl
);

public sealed record TeacherResearchProjectDto(
    string Id,
    string Type,
    string Name,
    string? ProjectCode,
    string? Institution,
    DateOnly StartDate,
    DateOnly? EndDate,
    int Months,
    string? Role,
    string? KnowledgeArea,
    string? Status,
    string? Country,
    string? SupportingDocumentUrl
);

public sealed record TeacherDoctoralThesisDto(
    string Id,
    string Type,
    string Title,
    string? Institution,
    DateOnly ApprovalDate,
    string Role,
    string? KnowledgeArea,
    string? Country,
    string? SupportingDocumentUrl
);

public sealed record TeacherLanguageDto(
    string Id,
    string Type,
    string Language,
    string Level,
    string? ReferenceFramework,
    string? CertifyingInstitution,
    string? Country,
    DateOnly IssueDate,
    DateOnly? ExpirationDate,
    string? SupportingDocumentUrl
);

public sealed record TeacherScoreDto(
    string Type,
    string Period,
    decimal Percentage,
    string? SupportingDocumentUrl
);

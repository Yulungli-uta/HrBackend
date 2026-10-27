namespace WsUtaSystem.Application.DTOs.AcademicPromotion;

// ── Constantes ───────────────────────────────────────────────────────────────────

public static class TrainingDirection
{
    // 2026-10-12: antes en inglés ("RECEIVED_TRAINING"/"GIVEN_TRAINING") -- el frontend
    // mostraba ese valor crudo porque leía ref_Types.Name directo. Se tradujo HR.ref_Types
    // (TypeId 2075/2076) a español; estas constantes deben coincidir EXACTO con ese valor,
    // ya que AcademicPromotionService.IsDirection() compara por igualdad de string contra
    // lo que devuelve la BD en vivo (ver GetRefTypeNamesAsync).
    public const string Received = "Capacitación recibida";
    public const string Given = "Capacitación impartida";
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
    string? Dedication,
    decimal? WeeklyHours,
    string? CurrentPosition,
    DateOnly? CurrentPositionStartDate,
    bool IsAuthority,
    IReadOnlyList<TeacherAuthorityDto> Authorities,
    DateOnly EvaluationDate,
    IReadOnlyList<TeacherExperienceDto> Experience,
    IReadOnlyList<TeacherPublicationDto> Publications,
    IReadOnlyList<TeacherTrainingDto> ReceivedTrainings,
    IReadOnlyList<TeacherTrainingDto> GivenTrainings,
    IReadOnlyList<ResearchProjectParticipationBlockDto> ResearchProjects,
    IReadOnlyList<TeacherDoctoralThesisDto> DoctoralTheses,
    IReadOnlyList<TeacherLanguageDto> Languages,
    TeacherScoreDto? Score
);

/// <summary>Autoridad institucional vigente del docente; solo cuenta hasta su fecha de fin.</summary>
public sealed record TeacherAuthorityDto(
    string Type,
    string Denomination,
    DateOnly StartDate,
    DateOnly? EndDate
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

// ── Participación en proyectos de investigación (bloque adicional del perfil) ───

public sealed record ResearchResolutionBlockDto(string Name, string? DocumentUrl);

public sealed record ResearchExtensionBlockDto(DateOnly EndDate, string ResolutionNumber, string? DocumentUrl);

public sealed record ResearchParticipationBlockDto(
    string Id,
    string Role,
    DateOnly StartDate,
    DateOnly EndDate);

/// <summary>Un proyecto de investigación con las participaciones del docente consultado.</summary>
public sealed record ResearchProjectParticipationBlockDto(
    string Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    ResearchResolutionBlockDto ApprovalResolution,
    ResearchResolutionBlockDto CompletionResolution,
    bool HasExtension,
    ResearchExtensionBlockDto? Extension,
    IReadOnlyList<ResearchParticipationBlockDto> Participations);

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

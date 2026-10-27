namespace WsUtaSystem.Application.DTOs.Trainings;

/// <summary>Certificado tal como lo entrega DEaDV (campos cert_*); solo uso interno del backend.</summary>
public sealed record DeadvCertificateDto(
    string Code,
    string? FileUrl,
    string? ValidatorCode,
    string? Area,
    string EventName,
    string? EventType,
    string? ApprovalType,
    string? Modality,
    string? Dependency,
    DateOnly StartDate,
    DateOnly EndDate,
    int Hours);

/// <summary>Estado de cada certificado frente a HR.tbl_Trainings.</summary>
public static class DeadvSyncStatus
{
    public const string New = "Nuevo";
    /// <summary>Existe un registro histórico equivalente sin origen: se completa en vez de duplicar.</summary>
    public const string Linkable = "Vinculable";
    public const string AlreadySynced = "YaSincronizado";
}

public sealed record DeadvSyncPreviewItemDto(
    string Code,
    string Title,
    string? Area,
    bool IsPedagogical,
    DateOnly StartDate,
    DateOnly EndDate,
    int Hours,
    string? Modality,
    string Status);

public sealed record DeadvSyncPreviewDto(
    int PersonId,
    bool ServiceAvailable,
    int TotalFound,
    int TotalNew,
    int TotalLinkable,
    int TotalAlreadySynced,
    IReadOnlyList<DeadvSyncPreviewItemDto> Items);

public sealed record DeadvSyncResultDto(
    int PersonId,
    int Created,
    int Linked,
    int Skipped);

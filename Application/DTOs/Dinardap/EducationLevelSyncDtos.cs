namespace WsUtaSystem.Application.DTOs.Dinardap;

public sealed record TituloSyncPreviewItemDto(
    string? NumeroRegistro,
    string NombreTitulo,
    string? Institucion,
    string? NivelNombreOriginal,
    bool YaExiste,
    bool RequiereRevision,
    string? MotivoRevision);

public sealed record TituloSyncPreviewDto(
    int PersonId,
    int TotalEncontrados,
    int TotalNuevos,
    int TotalYaExistentes,
    IReadOnlyList<TituloSyncPreviewItemDto> Items);

public sealed record TituloSyncResultDto(
    int PersonId,
    int TitulosCreados,
    int TitulosOmitidos,
    int RequierenRevision);

public sealed record BulkTituloSyncResultDto(
    int PersonasProcesadas,
    int PersonasConError,
    int TitulosCreadosTotal,
    int TitulosOmitidosTotal);

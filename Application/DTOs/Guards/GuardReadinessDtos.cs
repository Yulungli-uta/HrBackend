namespace WsUtaSystem.Application.DTOs.Guards;

/// <summary>
/// Resultado del chequeo de pre-requisitos del módulo de guardias para una fecha objetivo.
/// </summary>
public record GuardReadinessCheckDto(
    bool IsReady,
    List<GuardReadinessItemDto> Items
);

/// <summary>
/// Un ítem de pre-requisito con su estado y mensaje descriptivo.
/// </summary>
public record GuardReadinessItemDto(
    string Key,
    string Label,
    bool Passed,
    string? Detail
);

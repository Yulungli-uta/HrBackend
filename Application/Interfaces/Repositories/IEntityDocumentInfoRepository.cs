namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Número y fecha de un contrato o acción de personal, para ubicar sus documentos en disco.</summary>
/// <param name="Number">Número del documento (código del contrato o número de la acción); null si no tiene.</param>
/// <param name="DocumentDate">Fecha del documento: inicio del contrato o fecha de la acción.</param>
public sealed record EntityDocumentInfo(string? Number, DateOnly? DocumentDate);

/// <summary>
/// Lecturas mínimas que necesita la política de ubicación de documentos
/// (<c>IEntityDocumentLayoutPolicy</c>). Solo consulta; no modifica nada.
/// </summary>
public interface IEntityDocumentInfoRepository
{
    /// <summary>Código y fecha de inicio de un contrato; null si no existe o está borrado.</summary>
    Task<EntityDocumentInfo?> GetContractInfoAsync(int contractId, CancellationToken ct = default);

    /// <summary>Número y fecha de una acción de personal; null si no existe o está borrada.</summary>
    Task<EntityDocumentInfo?> GetPersonnelActionInfoAsync(int actionId, CancellationToken ct = default);

    /// <summary>Nombre del tipo de documento (<c>ref_Types</c>); null si no existe.</summary>
    Task<string?> GetDocumentTypeNameAsync(int documentTypeId, CancellationToken ct = default);
}

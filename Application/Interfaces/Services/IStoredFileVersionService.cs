using WsUtaSystem.Application.DTOs.StoredFileVersions;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Versionado genérico de archivos por ranura de un registro. Reemplazar no sobrescribe: crea una versión
/// nueva, deja la anterior inactiva y archivada, y registra quién, cuándo y por qué.
/// </summary>
public interface IStoredFileVersionService
{
    /// <summary>Versiones de una ranura (la vigente primero por número de versión descendente).</summary>
    Task<IReadOnlyList<StoredFileVersionDto>> GetVersionsAsync(string entityType, string entityId, string slotCode, CancellationToken ct);

    /// <summary>
    /// Registra <paramref name="storedFileId"/> como nueva versión vigente de la ranura. El archivo debe estar activo y
    /// pertenecer al mismo registro. Si ya hay una versión vigente, <paramref name="reason"/> es obligatorio.
    /// <paramref name="requireOwnership"/> exige que el archivo se haya subido para ese mismo registro; solo se omite en
    /// flujos que lo suben bajo otra entidad (ej. solicitud de renuncia) y ya lo validan por su lado.
    /// <paramref name="sameTransactionWork"/> se ejecuta dentro de la misma transacción (puntero propio del módulo).
    /// </summary>
    /// <returns>Número de la versión creada.</returns>
    Task<int> AppendVersionAsync(
        string entityType,
        string entityId,
        string slotCode,
        int storedFileId,
        int? userId,
        string? reason,
        bool requireOwnership,
        Func<CancellationToken, Task>? sameTransactionWork,
        CancellationToken ct);

    /// <summary>True si el archivo es hoy la versión vigente de alguna ranura (no debe eliminarse por otra vía).</summary>
    Task<bool> IsCurrentVersionFileAsync(int storedFileId, CancellationToken ct);
}

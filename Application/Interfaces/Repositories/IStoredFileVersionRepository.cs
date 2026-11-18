using WsUtaSystem.Application.DTOs.StoredFileVersions;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Acceso a datos de <c>HR.tbl_StoredFileVersions</c>.</summary>
public interface IStoredFileVersionRepository
{
    /// <summary>Todas las versiones de una ranura, de la más reciente a la más antigua, con nombres de quien cargó/reemplazó.</summary>
    Task<IReadOnlyList<StoredFileVersionDto>> GetVersionsAsync(string entityType, string entityId, string slotCode, CancellationToken ct);

    /// <summary>Archivo registrado (sin seguimiento) o null si no existe.</summary>
    Task<StoredFile?> GetStoredFileAsync(int storedFileId, CancellationToken ct);

    /// <summary>True si el archivo es la versión vigente de alguna ranura.</summary>
    Task<bool> IsCurrentVersionFileAsync(int storedFileId, CancellationToken ct);

    /// <summary>
    /// Agrega una versión nueva en una sola transacción: desactiva la vigente (quién/cuándo/motivo), archiva su
    /// <c>StoredFile</c> (Status = 3), inserta la nueva como vigente y ejecuta <paramref name="sameTransactionWork"/>
    /// (ej. mover el puntero del módulo). Devuelve el número de versión creado.
    /// </summary>
    Task<int> AppendVersionAsync(
        string entityType,
        string entityId,
        string slotCode,
        int storedFileId,
        int? userId,
        string? reason,
        Func<CancellationToken, Task>? sameTransactionWork,
        CancellationToken ct);
}

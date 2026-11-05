using WsUtaSystem.Application.DTOs.ScientificProviders;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Catálogo de proveedores de publicaciones científicas. Reutiliza ref_Types (categoría SCIENTIFIC_PROVIDER): el nombre
/// es el código estable y la configuración viaja en Metadata (JSON). Resuelve siempre por código, nunca por TypeID.
/// </summary>
public interface IScientificProviderCatalog
{
    /// <summary>Todos los proveedores (activos e inactivos), ordenados por SortOrder.</summary>
    Task<IReadOnlyList<ScientificProviderInfo>> GetAllAsync(CancellationToken ct);

    Task<ScientificProviderInfo?> GetByCodeAsync(string code, CancellationToken ct);

    Task<ScientificProviderInfo?> GetByTypeIdAsync(int typeId, CancellationToken ct);

    /// <summary>Como <see cref="GetByCodeAsync"/> pero lanza una excepción de negocio si el proveedor no existe.</summary>
    Task<ScientificProviderInfo> GetRequiredAsync(string code, CancellationToken ct);
}

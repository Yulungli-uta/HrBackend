namespace WsUtaSystem.Application.DTOs.ScientificProviders;

/// <summary>
/// Proveedor del catálogo (ref_Types, categoría SCIENTIFIC_PROVIDER) con su configuración (Metadata JSON).
/// </summary>
/// <param name="TypeId">TypeID en ref_Types (varía entre ambientes: el código resuelve por <see cref="Code"/>).</param>
/// <param name="Code">ref_Types.Name: código estable (SCOPUS, ORCID, WOS, MANUAL, MIGRACION).</param>
/// <param name="DisplayName">ref_Types.Description: texto para mostrar.</param>
/// <param name="IdLabel">Etiqueta del ID del investigador ("Scopus Author ID").</param>
/// <param name="IdPattern">Expresión regular de validación del ID (opcional).</param>
/// <param name="ProfileUrlTemplate">Enlace al perfil con <c>{id}</c> (opcional).</param>
/// <param name="RequiresProviderId">Para sincronizar hace falta un ID de este proveedor.</param>
/// <param name="AllowsMultipleIds">Una persona puede tener varios IDs (p. ej. perfiles de Scopus partidos).</param>
/// <param name="UsesProfileOrcid">Su ID es el ORCID del perfil: no se registra un ID aparte.</param>
/// <param name="RequiresApproval">Lo que trae pasa por la revisión de la Dirección de Investigación para contar.</param>
/// <param name="IsSystem">Origen de sistema (MANUAL, MIGRACION): no sincroniza ni se le registran IDs.</param>
/// <param name="HasAdapter">Existe un adaptador de sincronización en el backend (lo decide el código, no el catálogo).</param>
public sealed record ScientificProviderInfo(
    int TypeId,
    string Code,
    string DisplayName,
    bool IsActive,
    int SortOrder,
    string? IdLabel,
    string? IdPattern,
    string? ProfileUrlTemplate,
    bool RequiresProviderId,
    bool AllowsMultipleIds,
    bool UsesProfileOrcid,
    bool RequiresApproval,
    bool IsSystem,
    bool HasAdapter);

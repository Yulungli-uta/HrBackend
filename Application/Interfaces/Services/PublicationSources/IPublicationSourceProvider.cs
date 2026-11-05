namespace WsUtaSystem.Application.Interfaces.Services.PublicationSources;

/// <summary>
/// Adaptador de un proveedor de publicaciones científicas (Scopus, Web of Science…). Cada proveedor gestiona sus propias
/// consultas, autenticación, paginación y transformación de datos; el núcleo de sincronización solo conoce este contrato,
/// así que agregar un proveedor es una clase nueva más una fila en el catálogo (ref_Types, SCIENTIFIC_PROVIDER).
/// </summary>
public interface IPublicationSourceProvider
{
    /// <summary>Código del proveedor en el catálogo (ref_Types.Name), p. ej. <c>SCOPUS</c>.</summary>
    string ProviderCode { get; }

    /// <summary>true si la URL y las credenciales del proveedor están configuradas (Parámetros del Sistema).</summary>
    Task<bool> IsConfiguredAsync(CancellationToken ct);

    /// <summary>
    /// Recupera TODAS las publicaciones del investigador (recorre las páginas del proveedor). Nunca lanza por fallos del
    /// servicio externo: los devuelve como resultado parcial o con <see cref="ExternalPublicationsResult.Available"/> en false.
    /// </summary>
    Task<ExternalPublicationsResult> FetchPublicationsAsync(ExternalAuthorRef author, CancellationToken ct);
}

/// <summary>Cómo identificar al investigador ante el proveedor: su ID(s) en la plataforma y, según el proveedor, el ORCID.</summary>
public sealed record ExternalAuthorRef(string? Orcid, IReadOnlyList<string> ProviderAuthorIds);

/// <summary>Publicación ya transformada a un formato común, independiente del proveedor.</summary>
/// <param name="AffiliationNames">Afiliaciones que declara el proveedor para la publicación; vacío si no las informa (afiliación UTA desconocida).</param>
/// <param name="IsIndexed">true si el proveedor es una base indexada (Scopus); null si no se sabe (p. ej. ORCID).</param>
public sealed record ExternalPublication(
    string ExternalId,
    string Title,
    string? JournalName,
    string? Issn,
    string? Isbn,
    string? Volume,
    string? Issue,
    string? Pages,
    DateOnly? PublicationDate,
    string? Doi,
    string? SubtypeCode,
    string? AggregationType,
    IReadOnlyList<string> AffiliationNames,
    string? RecordUrl,
    bool? IsIndexed = null);

/// <param name="Available">false cuando el servicio externo no respondió, rechazó las credenciales o no está configurado.</param>
/// <param name="IsPartial">true si faltaron páginas (el resultado no es completo).</param>
/// <param name="QuotaRemaining">Cupo semanal restante informado por el proveedor (si lo informa).</param>
/// <param name="ErrorKind">Cuando no está disponible: <see cref="ExternalErrorKinds"/>. Permite a una corrida masiva decidir si seguir.</param>
/// <param name="SkippedByType">Publicaciones que el proveedor tiene pero no se importan por su tipo (libros, cartas, datos…).</param>
public sealed record ExternalPublicationsResult(
    IReadOnlyList<ExternalPublication> Items,
    int? TotalReported,
    bool IsPartial,
    bool Available,
    string? Message,
    int? QuotaRemaining = null,
    string? ErrorKind = null,
    int SkippedByType = 0);

/// <summary>Tipos de fallo del proveedor. CONFIG, CREDENTIALS y QUOTA afectan a todas las consultas: una corrida masiva se detiene.</summary>
public static class ExternalErrorKinds
{
    public const string Config = "CONFIG";
    public const string Credentials = "CREDENTIALS";
    public const string Quota = "QUOTA";
    public const string Network = "NETWORK";

    public static bool StopsBatch(string? kind) => kind is Config or Credentials or Quota;
}

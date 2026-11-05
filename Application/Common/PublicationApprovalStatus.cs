namespace WsUtaSystem.Application.Common;

/// <summary>
/// Estados de revisión de una publicación (HR.tbl_Publications.ApprovalStatus). Lista corta y estable: constante, no catálogo.
/// <list type="bullet">
/// <item><c>SIN_REVISION</c>: estado inicial de todo (histórico, manual y recién sincronizado).</item>
/// <item><c>APROBADA</c>: aprobada por la Dirección de Investigación; queda congelada.</item>
/// <item><c>RECHAZADA</c>: descartada; se conserva el ID externo para que no se reimporte.</item>
/// </list>
/// </summary>
public static class PublicationApprovalStatus
{
    public const string SinRevision = "SIN_REVISION";
    public const string Aprobada = "APROBADA";
    public const string Rechazada = "RECHAZADA";

    public static readonly IReadOnlyList<string> All = new[] { SinRevision, Aprobada, Rechazada };

    public static bool IsValid(string? status) => status is not null && All.Contains(status);
}

/// <summary>Códigos estables de los proveedores de publicaciones (ref_Types, categoría SCIENTIFIC_PROVIDER).</summary>
public static class ScientificProviderCodes
{
    public const string Category = "SCIENTIFIC_PROVIDER";
    public const string Scopus = "SCOPUS";
    public const string Orcid = "ORCID";
    public const string WebOfScience = "WOS";
    public const string Manual = "MANUAL";
    public const string Migration = "MIGRACION";
}

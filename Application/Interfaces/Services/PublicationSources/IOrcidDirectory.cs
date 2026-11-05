namespace WsUtaSystem.Application.Interfaces.Services.PublicationSources;

/// <summary>Consulta pública de ORCID: si un ORCID iD existe y qué nombre publica.</summary>
public interface IOrcidDirectory
{
    /// <summary>
    /// Busca el ORCID (ya validado en formato y dígito verificador). Nunca lanza por fallos del servicio externo: si no responde
    /// devuelve <see cref="OrcidLookupStatus.Unavailable"/>.
    /// </summary>
    Task<OrcidLookupResult> LookupAsync(string orcid, CancellationToken ct);
}

public enum OrcidLookupStatus
{
    Found,
    /// <summary>ORCID no tiene ningún registro con ese iD.</summary>
    NotFound,
    /// <summary>No se pudo consultar (red, límite de uso o error del servicio).</summary>
    Unavailable,
}

/// <param name="Status">Resultado de la consulta.</param>
/// <param name="GivenNames">Nombres que publica el registro; null si la persona no los hace públicos.</param>
/// <param name="FamilyName">Apellidos que publica el registro; null si no los hace públicos.</param>
public sealed record OrcidLookupResult(OrcidLookupStatus Status, string? GivenNames = null, string? FamilyName = null);

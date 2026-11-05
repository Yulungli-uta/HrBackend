using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Transforma una publicación de un proveedor externo (formato común) a la entidad de HR y completa los campos vacíos de
/// una publicación ya registrada. No sobrescribe nunca un dato existente.
/// </summary>
public static partial class PublicationMapper
{
    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonAlphanumeric();

    /// <summary>Entidad nueva con los datos del proveedor. El tipo de publicación lo resuelve el servicio (catálogo).</summary>
    public static Publications ToEntity(ExternalPublication ext, int personId, int providerTypeId, int? publicationTypeId, DateTime now)
    {
        var doi = DoiNormalizer.Normalize(ext.Doi);
        return new Publications
        {
            PersonId = personId,
            Title = Truncate(ext.Title.Trim(), 300)!,
            JournalName = Truncate(ext.JournalName, 200),
            Issn_Isbn = BuildIssnIsbn(ext.Issn, ext.Isbn),
            Volume = Truncate(ext.Volume, 50),
            JournalNumber = Truncate(ext.Issue, 50),
            Pages = Truncate(ext.Pages, 20),
            Doi = Truncate(doi, 255),
            Link = Truncate(DoiNormalizer.ToUrl(doi) ?? ext.RecordUrl, 500),
            PublicationDate = ext.PublicationDate,
            PublicationTypeId = publicationTypeId,
            IsIndexed = ext.IsIndexed,
            UTAffiliation = UtaAffiliation.Resolve(ext.AffiliationNames.ToList()),
            SourceProviderTypeId = providerTypeId,
            ExternalId = Truncate(ext.ExternalId, 60),
            ApprovalStatus = PublicationApprovalStatus.SinRevision,
            LastSyncedAt = now,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Copia a <paramref name="existing"/> los datos de <paramref name="mapped"/> solo donde <paramref name="existing"/> está
    /// vacío (Doi, Link, ISSN/ISBN, revista, volumen, número, páginas, fecha, tipo, indexada, afiliación UTA).
    /// Devuelve cuántos campos se completaron (o se completarían, con <paramref name="apply"/> en false).
    /// </summary>
    public static int CompleteEmptyFields(Publications existing, Publications mapped, bool apply)
    {
        var count = 0;

        void Fill(string? current, string? value, Action<string> set)
        {
            if (string.IsNullOrWhiteSpace(current) && !string.IsNullOrWhiteSpace(value))
            {
                count++;
                if (apply) set(value);
            }
        }

        Fill(existing.Doi, mapped.Doi, v => existing.Doi = v);
        Fill(existing.Link, mapped.Link, v => existing.Link = v);
        Fill(existing.Issn_Isbn, mapped.Issn_Isbn, v => existing.Issn_Isbn = v);
        Fill(existing.JournalName, mapped.JournalName, v => existing.JournalName = v);
        Fill(existing.Volume, mapped.Volume, v => existing.Volume = v);
        Fill(existing.JournalNumber, mapped.JournalNumber, v => existing.JournalNumber = v);
        Fill(existing.Pages, mapped.Pages, v => existing.Pages = v);

        if (existing.PublicationDate is null && mapped.PublicationDate is not null)
        {
            count++;
            if (apply) existing.PublicationDate = mapped.PublicationDate;
        }
        if (existing.PublicationTypeId is null && mapped.PublicationTypeId is not null)
        {
            count++;
            if (apply) existing.PublicationTypeId = mapped.PublicationTypeId;
        }
        if (existing.IsIndexed is null && mapped.IsIndexed is not null)
        {
            count++;
            if (apply) existing.IsIndexed = mapped.IsIndexed;
        }
        if (existing.UTAffiliation is null && mapped.UTAffiliation is not null)
        {
            count++;
            if (apply) existing.UTAffiliation = mapped.UTAffiliation;
        }
        return count;
    }

    /// <summary>ISSN de 8 dígitos como <c>XXXX-XXXX</c> (formato mayoritario en HR); si no hay ISSN, el primer ISBN. Máximo 20.</summary>
    public static string? BuildIssnIsbn(string? issn, string? isbn)
    {
        var digits = (issn ?? string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        if (digits.Length == 8) return $"{digits[..4]}-{digits[4..]}";
        if (!string.IsNullOrWhiteSpace(issn)) return Truncate(issn.Trim(), 20);
        return string.IsNullOrWhiteSpace(isbn) ? null : Truncate(isbn.Trim(), 20);
    }

    /// <summary>Minúsculas y sin tildes: para comparar nombres y títulos.</summary>
    public static string Fold(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    /// <summary>Título para comparar: sin tildes, sin signos y con espacios simples.</summary>
    public static string NormalizeTitle(string? title) =>
        NonAlphanumeric().Replace(Fold(title), " ").Trim();

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        return v.Length <= max ? v : v[..max];
    }
}

using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.PublicationSync;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Concilia una publicación del proveedor con las ya registradas de la persona. Orden de identidad:
/// 1) ID externo del mismo proveedor; 2) DOI normalizado; 3) título normalizado + año (con un año de diferencia entre la fecha
/// en línea del proveedor y la registrada a mano es solo un posible duplicado, no se asume igual). El DOI solo no basta (un DOI de libro
/// puede repetirse en varios capítulos): cuando hay coincidencia dudosa el resultado es ambiguo y lo decide una persona.
/// </summary>
public static class PublicationMatcher
{
    /// <summary>Similitud mínima de título para dar por buena una coincidencia de DOI.</summary>
    public const double DoiTitleSimilarity = 0.6;

    /// <summary>Similitud de título (con el mismo año) a partir de la cual se propone como posible duplicado.</summary>
    public const double FuzzyTitleSimilarity = 0.85;

    /// <summary>Diferencia máxima de años (fecha en línea frente a la de la revista) para proponer un posible duplicado.</summary>
    public const int NearYearTolerance = 1;

    /// <summary>
    /// Palabras mínimas que debe tener el título más corto para considerar que el otro lo contiene como inicio. ORCID suele
    /// guardar "Título en inglés,Título en español" en un solo título; con esto ese registro se reconoce como posible duplicado
    /// del que solo trae el título en inglés. Un mínimo evita que un título muy corto ("Introducción") coincida con cualquiera.
    /// </summary>
    public const int MinTokensForPrefixMatch = 4;

    public sealed record MatchOutcome(string Status, Publications? Matched, IReadOnlyList<Publications> Candidates);

    public static MatchOutcome Match(ExternalPublication ext, int providerTypeId, int? publicationTypeId, IReadOnlyList<Publications> existing)
    {
        // 1) Mismo proveedor, mismo ID del artículo
        var exact = existing.FirstOrDefault(p => p.SourceProviderTypeId == providerTypeId &&
            string.Equals(p.ExternalId, ext.ExternalId, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return Resolve(exact, ext, providerTypeId, publicationTypeId);

        // 2) DOI normalizado (se excluyen las filas ya vinculadas a otro artículo de este mismo proveedor)
        var doi = DoiNormalizer.Normalize(ext.Doi);
        if (doi is not null)
        {
            var byDoi = existing
                .Where(p => !(p.SourceProviderTypeId == providerTypeId && !string.IsNullOrEmpty(p.ExternalId)))
                .Where(p => DoiNormalizer.Normalize(p.Doi) == doi)
                .ToList();

            if (byDoi.Count > 0)
            {
                var similar = byDoi.Where(p => TitleSimilarity(p.Title, ext.Title) >= DoiTitleSimilarity).ToList();
                if (similar.Count == 1) return Resolve(similar[0], ext, providerTypeId, publicationTypeId);
                return new MatchOutcome(PublicationSyncItemStatus.Ambiguous, null, similar.Count > 1 ? similar : byDoi);
            }
        }

        // 3) Título normalizado + año
        var titleN = PublicationMapper.NormalizeTitle(ext.Title);
        var linkable = existing.Where(p => !(p.SourceProviderTypeId == providerTypeId && !string.IsNullOrEmpty(p.ExternalId))).ToList();

        var sameTitle = linkable
            .Where(p => PublicationMapper.NormalizeTitle(p.Title) == titleN && YearCompatible(p.PublicationDate, ext.PublicationDate))
            .ToList();
        if (sameTitle.Count == 1) return Resolve(sameTitle[0], ext, providerTypeId, publicationTypeId);
        if (sameTitle.Count > 1) return new MatchOutcome(PublicationSyncItemStatus.Ambiguous, null, sameTitle);

        // Mismo título con un año de diferencia, o título muy parecido en el mismo año o a un año: lo decide una persona.
        var near = linkable
            .Where(p => p.PublicationDate is not null && ext.PublicationDate is not null)
            .Where(p =>
            {
                var diff = Math.Abs(p.PublicationDate!.Value.Year - ext.PublicationDate!.Value.Year);
                if (diff > NearYearTolerance) return false;
                var existingTitle = PublicationMapper.NormalizeTitle(p.Title);
                if (existingTitle == titleN || OneStartsWithOther(existingTitle, titleN)) return true;
                return diff == 0 && TitleSimilarity(p.Title, ext.Title) >= FuzzyTitleSimilarity;
            })
            .ToList();
        if (near.Count > 0) return new MatchOutcome(PublicationSyncItemStatus.Ambiguous, null, near);

        return new MatchOutcome(PublicationSyncItemStatus.New, null, Array.Empty<Publications>());
    }

    /// <summary>true si uno de los títulos (ya normalizados) empieza con el otro, a nivel de palabra completa.</summary>
    private static bool OneStartsWithOther(string a, string b)
    {
        var (shortT, longT) = a.Length <= b.Length ? (a, b) : (b, a);
        if (shortT.Length == 0 || shortT.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < MinTokensForPrefixMatch) return false;
        return longT.StartsWith(shortT + " ", StringComparison.Ordinal);
    }

    /// <summary>Estado de una publicación ya identificada: congelada, con campos por completar o sin cambios.</summary>
    public static MatchOutcome Resolve(Publications matched, ExternalPublication ext, int providerTypeId, int? publicationTypeId)
    {
        if (PublicationRules.IsLocked(matched.ApprovalStatus))
            return new MatchOutcome(PublicationSyncItemStatus.Locked, matched, Array.Empty<Publications>());

        var mapped = PublicationMapper.ToEntity(ext, matched.PersonId, providerTypeId, publicationTypeId, default);
        var fills = PublicationMapper.CompleteEmptyFields(matched, mapped, apply: false);
        return new MatchOutcome(
            fills > 0 ? PublicationSyncItemStatus.Completable : PublicationSyncItemStatus.Existing,
            matched, Array.Empty<Publications>());
    }

    /// <summary>Similitud de Jaccard entre las palabras de dos títulos (0 a 1), ignorando tildes y signos.</summary>
    public static double TitleSimilarity(string? a, string? b)
    {
        var sa = Tokens(a);
        var sb = Tokens(b);
        if (sa.Count == 0 || sb.Count == 0) return 0;
        var inter = sa.Intersect(sb).Count();
        var union = sa.Union(sb).Count();
        return union == 0 ? 0 : (double)inter / union;
    }

    private static HashSet<string> Tokens(string? title) =>
        PublicationMapper.NormalizeTitle(title).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2).ToHashSet(StringComparer.Ordinal);

    private static bool YearCompatible(DateOnly? a, DateOnly? b) =>
        a is null || b is null || a.Value.Year == b.Value.Year;
}

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <summary>Implementación de <see cref="IEntityDocumentLayoutPolicy"/>. Ver la interfaz para el esquema de carpetas.</summary>
public sealed class EntityDocumentLayoutPolicy : IEntityDocumentLayoutPolicy
{
    private const string ContractEntityType = "HRCONTRACT";
    private const string PersonnelActionEntityType = "PersonnelAction";
    private const int MaxStemLength = 120;

    private static readonly Regex ForbiddenChars = new(@"[\\/:*?""<>|\x00-\x1f]", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NonTypeChars = new(@"[^A-Za-z0-9]+", RegexOptions.Compiled);
    private static readonly Regex TrailingYear = new(@"(?<!\d)(19[89]\d|20[0-3]\d)\s*$", RegexOptions.Compiled);

    private readonly IEntityDocumentInfoRepository _info;
    private readonly IAppClock _clock;

    public EntityDocumentLayoutPolicy(IEntityDocumentInfoRepository info, IAppClock clock)
    {
        _info = info ?? throw new ArgumentNullException(nameof(info));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<DocumentLayout?> ResolveAsync(
        string entityType,
        string entityId,
        string? documentRole,
        int? documentTypeId,
        string originalFileName,
        CancellationToken ct = default)
    {
        if (!int.TryParse(entityId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) || id <= 0)
            return null;

        EntityDocumentInfo? info;
        string fallbackNumber;
        switch (entityType)
        {
            case ContractEntityType:
                info = await _info.GetContractInfoAsync(id, ct);
                fallbackNumber = $"CON-{id}";
                break;
            case PersonnelActionEntityType:
                info = await _info.GetPersonnelActionInfoAsync(id, ct);
                fallbackNumber = $"ACC-{id}";
                break;
            default:
                return null;
        }

        if (info is null) return null;

        var typeName = documentTypeId.HasValue ? await _info.GetDocumentTypeNameAsync(documentTypeId.Value, ct) : null;
        var role = ResolveRole(documentRole, typeName);

        var number = string.IsNullOrWhiteSpace(info.Number) ? fallbackNumber : info.Number.Trim();
        var numberStem = SafeStem(number, fallbackNumber);
        var year = ResolveYear(info.DocumentDate, number);
        var extension = Path.GetExtension(originalFileName ?? string.Empty).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension)) extension = ".bin";

        var y = year.ToString(CultureInfo.InvariantCulture);
        return role switch
        {
            DocumentRoles.Signed => new DocumentLayout(y, numberStem + extension, year),
            DocumentRoles.Cancellation => new DocumentLayout($"{y}/anulacion", numberStem + extension, year),
            DocumentRoles.Resignation => new DocumentLayout($"{y}/renuncia", numberStem + extension, year),
            _ => new DocumentLayout($"{y}/anexos/{numberStem}", TypeStem(typeName) + extension, year),
        };
    }

    /// <summary>Rol explícito del cliente; si no hay (o es anexo), lo deduce del nombre del tipo de documento (anulación / renuncia).</summary>
    private static string ResolveRole(string? requestedRole, string? documentTypeName)
    {
        var role = DocumentRoles.Normalize(requestedRole);
        if (role is not null && role != DocumentRoles.Attachment) return role;

        if (!string.IsNullOrWhiteSpace(documentTypeName))
        {
            var folded = RemoveDiacritics(documentTypeName).ToUpperInvariant();
            if (folded.Contains("ANULA", StringComparison.Ordinal)) return DocumentRoles.Cancellation;
            if (folded.Contains("RENUNCIA", StringComparison.Ordinal)) return DocumentRoles.Resignation;
        }

        return DocumentRoles.Attachment;
    }

    /// <summary>
    /// Año de la carpeta: el de la fecha del documento; si esa fecha difiere más de un año del año del número (datos migrados con
    /// fechas erróneas) o no existe, el año del número; sin número con año, el año actual.
    /// </summary>
    private int ResolveYear(DateOnly? documentDate, string number)
    {
        var match = TrailingYear.Match(number);
        int? numberYear = match.Success ? int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) : null;

        if (documentDate.HasValue)
        {
            var y = documentDate.Value.Year;
            if (numberYear is null || Math.Abs(y - numberYear.Value) <= 1) return y;
            return numberYear.Value;
        }

        return numberYear ?? _clock.Today.Year;
    }

    /// <summary>Nombre seguro a partir del número tal como está: solo cambian los caracteres que Windows no acepta y los espacios.</summary>
    private static string SafeStem(string value, string fallback)
    {
        var s = ForbiddenChars.Replace(value, "-");
        s = Whitespace.Replace(s, "_");
        s = s.Trim(' ', '.', '_');
        if (s.Length == 0) s = fallback;
        return s.Length > MaxStemLength ? s[..MaxStemLength] : s;
    }

    /// <summary>Nombre de un anexo a partir de su tipo de documento: sin acentos, en mayúsculas y con guiones bajos.</summary>
    private static string TypeStem(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return "ANEXO";
        var s = NonTypeChars.Replace(RemoveDiacritics(typeName).ToUpperInvariant(), "_").Trim('_');
        return s.Length == 0 ? "ANEXO" : (s.Length > MaxStemLength ? s[..MaxStemLength] : s);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

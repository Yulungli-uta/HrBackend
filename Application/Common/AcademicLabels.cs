namespace WsUtaSystem.Application.Common;

/// <summary>
/// Denominaciones para mostrar de los catálogos académicos (ref_Types ACADEMIC_CATEGORY / ACADEMIC_LEVEL /
/// ACADEMIC_DEDICATION). En BD el <c>Name</c> es un código estable; aquí se traduce al texto institucional.
/// Un código desconocido se devuelve legible, nunca vacío.
/// </summary>
public static class AcademicLabels
{
    private static readonly Dictionary<string, string> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TITULAR_PRINCIPAL"] = "Titular Principal",
        ["TITULAR_AGREGADO"]  = "Titular Agregado",
        ["TITULAR_AUXILIAR"]  = "Titular Auxiliar",
        ["OCASIONAL"]         = "Ocasional",
        ["INVITADO"]          = "Invitado",
        ["HONORARIO"]         = "Honorario",
        ["EMERITO"]           = "Emérito",
    };

    private static readonly Dictionary<string, string> Dedications = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TC"]        = "Tiempo Completo",
        ["MT"]        = "Medio Tiempo",
        ["HORAS"]     = "Tiempo Parcial",
        ["EXCLUSIVA"] = "Exclusiva",
    };

    public static string Category(string? name) => Lookup(Categories, name);

    public static string Dedication(string? name) => Lookup(Dedications, name);

    /// <summary>NIVEL_2 → "Nivel 2".</summary>
    public static string Level(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "—";
        return name.StartsWith("NIVEL_", StringComparison.OrdinalIgnoreCase)
            ? $"Nivel {name["NIVEL_".Length..]}"
            : Humanize(name);
    }

    private static string Lookup(Dictionary<string, string> map, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "—";
        return map.TryGetValue(name, out var label) ? label : Humanize(name);
    }

    private static string Humanize(string code) =>
        string.Join(' ', code.ToLowerInvariant().Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
}

using System.Text.RegularExpressions;

namespace WsUtaSystem.Application.DTOs.UserPreferences;

/// <summary>
/// Preferencias de interfaz del usuario. Todos los campos son opcionales: un valor nulo
/// significa "usar el valor por defecto de la aplicación". No contiene datos personales
/// ni información de salud/discapacidad.
/// </summary>
public sealed class UserPreferencesDto
{
    /// <summary>Ruta interna de la página de inicio (ej. "/attendance"). Nula = "/".</summary>
    public string? HomePath { get; set; }
    /// <summary>Menú en escritorio: "remember" | "expanded" | "collapsed".</summary>
    public string? MenuStartMode { get; set; }
    /// <summary>Último estado del menú en escritorio (se usa con "remember").</summary>
    public bool? MenuCollapsed { get; set; }
    /// <summary>Escala de texto: "small" | "normal" | "large" | "xlarge".</summary>
    public string? FontScale { get; set; }
    /// <summary>Tema: "light" | "dark" | "system".</summary>
    public string? Theme { get; set; }
    /// <summary>Densidad: "standard" | "compact".</summary>
    public string? Density { get; set; }
    public bool? HighContrast { get; set; }
    public bool? ReduceMotion { get; set; }
}

/// <summary>Respuesta de GET/PUT: preferencias + metadatos de versión.</summary>
public sealed class UserPreferencesResponseDto
{
    public int SchemaVersion { get; set; }
    public UserPreferencesDto Preferences { get; set; } = new();
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Reglas de validación de las preferencias (lista blanca de valores).</summary>
public static class UserPreferencesValidator
{
    public const short CurrentSchemaVersion = 1;

    private static readonly string[] MenuModes = { "remember", "expanded", "collapsed" };
    private static readonly string[] FontScales = { "small", "normal", "large", "xlarge" };
    private static readonly string[] Themes = { "light", "dark", "system" };
    private static readonly string[] Densities = { "standard", "compact" };

    // Solo rutas internas relativas: "/", segmentos alfanuméricos con - _ . y /, sin "//" ni esquema.
    private static readonly Regex HomePathRegex =
        new(@"^/([A-Za-z0-9._~-]+(/[A-Za-z0-9._~-]+)*)?$", RegexOptions.Compiled);

    /// <summary>Devuelve la lista de errores de validación (vacía si es válido).</summary>
    public static List<string> Validate(UserPreferencesDto dto)
    {
        var errors = new List<string>();
        if (dto.HomePath is not null)
        {
            if (dto.HomePath.Length > 200 || !HomePathRegex.IsMatch(dto.HomePath) || dto.HomePath.Contains(".."))
                errors.Add("La página de inicio no es una ruta interna válida.");
        }
        if (dto.MenuStartMode is not null && !MenuModes.Contains(dto.MenuStartMode))
            errors.Add("El modo de inicio del menú no es válido.");
        if (dto.FontScale is not null && !FontScales.Contains(dto.FontScale))
            errors.Add("El tamaño de texto no es válido.");
        if (dto.Theme is not null && !Themes.Contains(dto.Theme))
            errors.Add("El tema no es válido.");
        if (dto.Density is not null && !Densities.Contains(dto.Density))
            errors.Add("La densidad no es válida.");
        return errors;
    }
}

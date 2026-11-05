namespace WsUtaSystem.Application.Common;

/// <summary>
/// Identifica los parámetros del sistema que guardan credenciales (claves de API, tokens, contraseñas). Se reconocen por el
/// nombre porque la tabla de parámetros no tiene un tipo de dato para secretos. Sus valores no se devuelven nunca por la API
/// de Parámetros (solo si tienen valor) y al editarlos un valor vacío conserva el actual; el backend sí los lee internamente.
/// </summary>
public static class ParameterSecrecy
{
    private static readonly string[] SecretSuffixes = { "_API_KEY", "_TOKEN", "_SECRET", "_PASSWORD" };

    public static bool IsSecret(string? parameterName) =>
        !string.IsNullOrWhiteSpace(parameterName) &&
        SecretSuffixes.Any(s => parameterName.Trim().EndsWith(s, StringComparison.OrdinalIgnoreCase));
}

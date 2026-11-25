using System.Text.Json;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Validación del campo <c>Metadata</c> de un tipo de referencia cuando llega nuevo desde la API: vacío = sin datos (null);
/// si trae texto debe ser un JSON válido, para que quien lo lea (p. ej. el prefijo de nivel de los grupos ocupacionales) no falle.
/// Solo se aplica a lo recibido, nunca al valor ya guardado de otros tipos.
/// </summary>
public static class RefTypeMetadata
{
    /// <summary>Devuelve null si está vacío o el mismo texto si es JSON válido; si no lo es, lanza una regla de negocio.</summary>
    public static string? Normalize(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata)) return null;
        try
        {
            using var _ = JsonDocument.Parse(metadata);
            return metadata;
        }
        catch (JsonException)
        {
            throw new BusinessRuleException("Los datos adicionales (metadata) no tienen un formato JSON válido.");
        }
    }
}

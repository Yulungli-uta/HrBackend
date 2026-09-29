using System.Text.Json;
using System.Text.Json.Serialization;

namespace WsUtaSystem.Infrastructure.Serialization;

/// <summary>
/// Convierte "" (string vacío) a null para DateOnly? — por defecto, System.Text.Json rechaza
/// un string vacío para un tipo fecha con FormatException, que ValidateModelFilter reporta
/// como "Solicitud inválida" sin más detalle. El frontend a veces normaliza un campo de fecha
/// opcional a "" en vez de omitir la clave (ej. "Trabajo actual" sin fecha de fin en
/// WorkExperienceForm — hallazgo informe UTA-DITIC-PS-027-2026, observación 26); en vez de
/// perseguir cada formulario que pueda repetir el patrón, se tolera a nivel de serialización
/// para toda la API. Nunca se propone null cuando el string no está vacío: eso sigue siendo
/// un error de formato real.
/// </summary>
public sealed class EmptyStringTolerantDateOnlyConverter : JsonConverter<DateOnly?>
{
    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value)) return null;
            return DateOnly.Parse(value);
        }
        throw new JsonException($"No se pudo convertir el valor a fecha (DateOnly).");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        else writer.WriteNullValue();
    }
}

/// <summary>Mismo tratamiento que <see cref="EmptyStringTolerantDateOnlyConverter"/> para DateTime?.</summary>
public sealed class EmptyStringTolerantDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value)) return null;
            return DateTime.Parse(value, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }
        throw new JsonException($"No se pudo convertir el valor a fecha (DateTime).");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteStringValue(value.Value.ToString("o"));
        else writer.WriteNullValue();
    }
}

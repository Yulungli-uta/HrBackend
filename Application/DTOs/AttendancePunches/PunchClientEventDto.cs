namespace WsUtaSystem.Application.DTOs.AttendancePunches;

/// <summary>
/// Evento que la pantalla de picadas informa al servidor para poder reconstruir después qué ocurrió en el dispositivo
/// (por ejemplo, si el usuario llegó a pulsar el botón aunque la picada nunca se haya creado).
/// </summary>
public sealed class PunchClientEventDto
{
    /// <summary>CLICK, RETRY, SEND_FAILED o HIDDEN_BEFORE_RESPONSE.</summary>
    public string Event { get; set; } = string.Empty;

    /// <summary>Detalle corto opcional (máximo 200 caracteres): estado de la conexión, código de error, etc.</summary>
    public string? Detail { get; set; }
}

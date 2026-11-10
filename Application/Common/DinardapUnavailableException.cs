namespace WsUtaSystem.Application.Common;

/// <summary>
/// DINARDAP no pudo atender la consulta (servicio caído, sin respuesta de SENESCYT, configuración o token de servicio).
/// Se distingue de "la persona no tiene datos" para que la pantalla no muestre una lista vacía como si fuera un resultado real.
/// </summary>
public sealed class DinardapUnavailableException : Exception
{
    public DinardapUnavailableException(string message) : base(message) { }
}

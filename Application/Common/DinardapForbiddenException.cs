namespace WsUtaSystem.Application.Common;

/// <summary>
/// DINARDAP rechazó la consulta porque el usuario que la pidió no tiene el permiso de consulta (rol R_DINARDAP_REGISTRO_CIVIL o R_DINARDAP_TITULOS, según el paquete).
/// Se distingue de "servicio no disponible" para que la pantalla pueda decir qué falta y no entregar un resultado vacío.
/// </summary>
public sealed class DinardapForbiddenException : Exception
{
    public DinardapForbiddenException(string message) : base(message) { }
}

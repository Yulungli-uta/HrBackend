namespace WsUtaSystem.Infrastructure.Common;

/// <summary>
/// Convención compartida del caché en memoria de vw_EmployeeDetails entre
/// JwtAuthenticationMiddleware y CurrentUserService. Centraliza la clave y el TTL
/// para que ambos consumidores lean/escriban la MISMA entrada y con la MISMA vigencia:
/// una fila sembrada por el middleware (fallback por email) es reutilizada por
/// CurrentUserService sin volver a consultar la vista.
/// </summary>
public static class EmployeeDetailsCache
{
    // La clave se construye solo con el EmployeeID (int) ya validado:
    // nunca con texto controlable por el cliente, para evitar colisiones o envenenamiento.
    private const string KeyPrefix = "auth:employee-details:";

    private const int DefaultCacheMinutes = 3;

    /// <summary>Clave de caché para los detalles de un empleado.</summary>
    public static string KeyFor(int employeeId) => $"{KeyPrefix}{employeeId}";

    /// <summary>
    /// TTL del caché leído de "AuthService:EmployeeCacheMinutes" (default 3 minutos).
    /// Único punto de lectura: todos los que siembran la entrada usan la misma vigencia.
    /// </summary>
    public static TimeSpan GetDuration(IConfiguration configuration) =>
        int.TryParse(configuration["AuthService:EmployeeCacheMinutes"], out var minutes) && minutes > 0
            ? TimeSpan.FromMinutes(minutes)
            : TimeSpan.FromMinutes(DefaultCacheMinutes);
}

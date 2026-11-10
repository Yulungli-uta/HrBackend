namespace WsUtaSystem.Application.DTOs.Dinardap;

/// <summary>Paquetes de DINARDAP que se registran en la auditoría.</summary>
public static class DinardapPackages
{
    public const string RegistroCivil = "REGISTRO_CIVIL";
    public const string Titulos = "TITULOS";
    public const string Tce = "TCE";
}

/// <summary>Resultados posibles de una consulta registrada.</summary>
public static class DinardapQueryOutcomes
{
    /// <summary>El servicio respondió (aunque la persona no tuviera datos).</summary>
    public const string Ok = "OK";
    /// <summary>No se pudo consultar (configuración, token, red o rechazo del servicio).</summary>
    public const string Unavailable = "UNAVAILABLE";
    /// <summary>DINARDAP rechazó al usuario por falta de permiso (rol R_DINARDAP_REGISTRO_CIVIL o R_DINARDAP_TITULOS, según el paquete).</summary>
    public const string Forbidden = "FORBIDDEN";
}

/// <summary>Con qué identidad se llama a DINARDAP.</summary>
public enum DinardapIdentity
{
    /// <summary>Token del usuario de la solicitud (pantallas). Sin usuario, o con la opción desactivada, usa la cuenta de servicio.</summary>
    CurrentUser,
    /// <summary>Siempre la cuenta de servicio (procesos masivos o en segundo plano).</summary>
    Service,
}

/// <summary>Resultado de una consulta a DINARDAP.</summary>
public enum DinardapLookupStatus
{
    /// <summary>El servicio respondió (aunque la persona no tuviera datos).</summary>
    Ok,
    /// <summary>No se pudo consultar (configuración, token, red o rechazo del servicio).</summary>
    Unavailable,
    /// <summary>El usuario no tiene el permiso de consulta en DINARDAP.</summary>
    Forbidden,
}

/// <summary>Resultado de una consulta: los datos (si los hay) y por qué no los hay.</summary>
public sealed record DinardapLookupResult<T>(T? Data, DinardapLookupStatus Status) where T : class;

/// <summary>Origen de la consulta: un usuario conectado o un proceso del sistema (trabajos programados).</summary>
public static class DinardapQueryOrigins
{
    public const string User = "USER";
    public const string System = "SYSTEM";
}

/// <summary>Consulta a DINARDAP por registrar. La identificación llega completa y se enmascara al guardar.</summary>
public sealed record DinardapQueryRecord(
    string Origin,
    Guid? UserId,
    string? UserEmail,
    int? EmployeeId,
    string Package,
    string Identification,
    string Outcome,
    int DurationMs,
    string? SourceIp,
    string? Context);

/// <summary>Filtros del listado de consultas a DINARDAP.</summary>
public sealed record DinardapQueryLogFilter(
    DateOnly? From,
    DateOnly? To,
    string? User,
    string? Package,
    string? Origin,
    string? Outcome);

/// <summary>Consulta registrada, tal como se muestra en el listado (la identificación siempre va enmascarada).</summary>
public sealed record DinardapQueryLogItemDto(
    long Id,
    DateTime QueriedAt,
    string Origin,
    Guid? UserId,
    string? UserEmail,
    string? UserFullName,
    int? EmployeeId,
    string Package,
    string IdentificationMasked,
    string Outcome,
    int? DurationMs,
    string? SourceIp,
    string? Context);

using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Dinardap;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Auditoría de las consultas que HrBackend hace a DINARDAP: deja constancia de quién las pidió (usuario conectado o
/// proceso del sistema), el paquete, la identificación enmascarada, la fecha y el resultado.
/// </summary>
public interface IDinardapQueryAuditService
{
    /// <summary>
    /// Registra una consulta. Nunca lanza excepción: si no se puede escribir el registro se deja una advertencia en el log
    /// y la consulta del usuario sigue su curso.
    /// </summary>
    Task RecordAsync(DinardapQueryRecord record, CancellationToken ct = default);

    /// <summary>Listado paginado de consultas registradas, de la más reciente a la más antigua.</summary>
    Task<PagedResult<DinardapQueryLogItemDto>> SearchAsync(DinardapQueryLogFilter filter, int page, int pageSize, CancellationToken ct = default);
}

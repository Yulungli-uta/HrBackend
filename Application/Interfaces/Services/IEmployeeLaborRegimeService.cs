using WsUtaSystem.Application.DTOs.EmployeeLaborRegime;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IEmployeeLaborRegimeService
{
    Task<List<EmployeeLaborRegimeDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Agrega un nuevo régimen activo al empleado y recalcula cuál queda como principal
    /// (nombramiento gana; si ninguno es nombramiento, gana LOSEP).
    /// </summary>
    Task<EmployeeLaborRegimeDto> CreateAsync(EmployeeLaborRegimeCreateDto dto, int? changedBy, CancellationToken ct = default);

    /// <summary>
    /// Cierra (desactiva) un régimen existente y recalcula el principal entre los que queden activos.
    /// Retorna null si el id no existe.
    /// </summary>
    Task<EmployeeLaborRegimeDto?> CloseAsync(int id, EmployeeLaborRegimeCloseDto dto, int? changedBy, CancellationToken ct = default);

    /// <summary>
    /// Clasifica (o corrige) el campo SIIES INGRESO_POR_CONCURSO de un régimen existente.
    /// Retorna null si el id no existe.
    /// </summary>
    Task<EmployeeLaborRegimeDto?> SetIngresoPorConcursoAsync(int id, EmployeeLaborRegimeIngresoPorConcursoDto dto, int? changedBy, CancellationToken ct = default);
}

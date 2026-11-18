using WsUtaSystem.Application.DTOs.EmployeeLaborRegime;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IEmployeeLaborRegimeService
{
    Task<List<EmployeeLaborRegimeDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Unión de regímenes laborales activos entre todos los empleados dados. Un empleado
    /// puede aportar más de uno (ej. nombramiento LOSEP + contrato Código de Trabajo).
    /// </summary>
    Task<List<int>> GetActiveRegimeIdsForEmployeesAsync(List<int> employeeIds, CancellationToken ct = default);

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

    /// <summary>
    /// Fija o quita la relación IES manual (SIIES RELACION_IES) de un régimen existente. Retorna null si el id no existe;
    /// lanza <c>BusinessRuleException</c> si el TypeId no pertenece a la categoría SIIES_RELACION_IES.
    /// </summary>
    Task<EmployeeLaborRegimeDto?> SetSiiesRelacionIesAsync(int id, EmployeeLaborRegimeSiiesRelacionIesDto dto, int? changedBy, CancellationToken ct = default);

    /// <summary>
    /// Replica el departamento y el cargo de destino de una acción de personal VIGENTE/FINALIZADA (típicamente tras una
    /// corrección) en el régimen laboral que nació de ella. Si la acción no tiene régimen y hay un cambio de departamento o
    /// cargo respecto de la posición actual del empleado, lo crea (solo VIGENTE). No toca nada si el empleado tiene una
    /// acción posterior con otro departamento o cargo. No lanza por reglas de negocio: devuelve el resultado.
    /// </summary>
    Task<LaborRegimeSyncResult> SyncPositionFromPersonnelActionAsync(int actionId, int? changedBy, string? previousActionNumber = null, CancellationToken ct = default);

    /// <summary>
    /// Replica el departamento, cargo, número y fechas de un contrato corregido en el régimen laboral que nació de él
    /// (<c>SourceContractId</c>). El fin del régimen solo se mueve si coincidía con la fecha de fin anterior del contrato
    /// (<paramref name="previousEndDate"/>). No lanza por reglas de negocio: devuelve el resultado.
    /// </summary>
    Task<LaborRegimeSyncResult> SyncFromContractAsync(int contractId, DateTime? previousEndDate, int? changedBy, string? previousContractCode = null, CancellationToken ct = default);

    /// <summary>
    /// Registra un documento de relación laboral (contrato, adendum o acción) como una fila del historial del empleado y lo
    /// encadena: si ya hay un régimen activo del mismo tipo que empezó antes, lo cierra el día anterior y el nuevo queda activo;
    /// si el documento es anterior al activo, queda cerrado hasta donde empieza el siguiente. Idempotente: si el documento ya
    /// tiene fila, la devuelve sin duplicarla. A diferencia de <c>CreateAsync</c>, no falla por tener ya el régimen activo.
    /// </summary>
    Task<EmployeeLaborRegimeDto> RegisterDocumentAsync(EmployeeLaborRegimeCreateDto dto, int? changedBy, CancellationToken ct = default);

    /// <summary>true si el empleado tiene un régimen activo de ese tipo respaldado por un contrato.</summary>
    Task<bool> HasActiveContractRegimeAsync(int employeeId, int laborRegimeId, CancellationToken ct = default);
}

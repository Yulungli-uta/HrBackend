using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.MassVacationPlan;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IMassVacationPlanService
{
    Task<List<MassVacationPlanDto>> GetAllAsync(CancellationToken ct);

    /// <summary>Listado paginado con búsqueda por descripción y filtro por rango de fechas (solapamiento).</summary>
    Task<PagedResult<MassVacationPlanDto>> GetPagedAsync(
        int page, int pageSize, string? search, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct);

    Task<MassVacationPlanDto?> GetByIdAsync(int planId, CancellationToken ct);

    /// <summary>Crea el plan en estado Planificado. StartDate debe ser estrictamente futura;
    /// si se especifica StartTime/EndTime (modo "por horas"), StartDate debe ser igual a EndDate.</summary>
    Task<MassVacationPlanDto> CreateAsync(MassVacationPlanCreateDto dto, int? createdByEmployeeId, CancellationToken ct);

    /// <summary>Edita un plan mientras está en PLANNED, con las mismas validaciones que CreateAsync.</summary>
    Task<MassVacationPlanDto> UpdateAsync(int planId, MassVacationPlanUpdateDto dto, int? performedByEmployeeId, CancellationToken ct);

    Task<List<MassVacationPlanRosterItemDto>> GetRosterAsync(int planId, CancellationToken ct);

    /// <summary>Marca/desmarca a un empleado como excluido (trabaja normalmente) del plan.
    /// Solo permitido mientras el plan está en Planificado.</summary>
    Task SetExclusionAsync(int planId, MassVacationPlanExclusionSetDto dto, int? performedByEmployeeId, CancellationToken ct);

    /// <summary>Cancela un plan (Anulado). Solo permitido mientras está en Planificado —
    /// una vez En Ejecución ya se descontó saldo, no se puede anular.</summary>
    Task CancelAsync(int planId, string? reason, int? performedByEmployeeId, CancellationToken ct);

    /// <summary>
    /// Llamado por el job diario (DailyMassVacationPlanTransitionJob): pasa a En Ejecución
    /// (con descuento de saldo) los planes Planificados cuya fecha de inicio ya llegó, y a
    /// Finalizado los En Ejecución cuya fecha de fin ya pasó. Cada plan se procesa en su
    /// propia transacción — si uno falla no bloquea a los demás.
    /// </summary>
    Task<MassVacationPlanTransitionRunResultDto> ProcessDueTransitionsAsync(int? performedByEmployeeId, CancellationToken ct);

    /// <summary>
    /// Planes ejecutados/finalizados que le aplican a este empleado (institucional o su
    /// departamento) y de los que NO está excluido — para mostrarlos como "Vacación
    /// Institucional" en el historial personal de vacaciones, sin que exista una fila en
    /// HR.tbl_Vacations.
    /// </summary>
    Task<List<MassVacationPlanDto>> GetApplicablePlansForEmployeeAsync(int employeeId, CancellationToken ct);
}

using WsUtaSystem.Application.DTOs.TimeBalances;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Ajuste administrativo de saldo de vacaciones (incrementar/descontar/establecer),
/// distinto de la acreditación automática mensual — no recalcula ni reinterpreta
/// acreditaciones pasadas, solo mueve el saldo actual con trazabilidad.
/// </summary>
public interface IVacationBalanceAdjustmentService
{
    Task<VacationBalanceAdjustmentResultDto> AdjustAsync(
        VacationBalanceAdjustmentDto dto, int? performedByEmpId, CancellationToken ct = default);

    Task<List<VacationBalanceBulkAdjustmentRowResultDto>> BulkAdjustAsync(
        VacationBalanceBulkAdjustmentRequestDto request, int? performedByEmpId, CancellationToken ct = default);

    /// <summary>Buzón: regímenes cerrados (contrato/relación terminada) con saldo aún sin liquidar.</summary>
    Task<List<PendingVacationSettlementDto>> GetPendingSettlementsAsync(CancellationToken ct = default);

    /// <summary>Saldo actual (vacaciones + recuperación) de un empleado en un régimen específico — 0/0 si todavía no tiene fila.</summary>
    Task<CurrentTimeBalanceDto> GetCurrentBalanceAsync(int employeeId, string laborRegimeName, CancellationToken ct = default);

    /// <summary>
    /// Liquida el saldo de un régimen ya cerrado: fija en 0 tanto vacaciones como
    /// recuperación de horas (pueden venir de positivo o de negativo — ambos casos
    /// válidos), en una sola transacción, y deja el movimiento auditado de cada bolsa con
    /// el monto real liquidado, para que RRHH/nómina lo use como referencia del pago o
    /// descuento final.
    /// </summary>
    Task<VacationSettlementResultDto> SettleAsync(
        VacationSettlementRequestDto request, int? performedByEmpId, CancellationToken ct = default);

    /// <summary>
    /// Reserva (descuenta) minutos del régimen laboral principal activo del empleado —
    /// reemplaza a HR.sp_hr_ReserveVacationBalance/sp_hr_ReservePermissionBalance.
    /// Bloquea siempre si el saldo resultante quedaría negativo (allowNegativeResult=false,
    /// no configurable — una reserva nunca debe sobregirar). Idempotente por sourceId: si ya
    /// existe una reserva con ese identificador, lanza BusinessRuleException.
    /// </summary>
    Task<VacationBalanceAdjustmentResultDto> ReserveAsync(
        int employeeId, DTOs.TimeBalances.TimeBalanceField field, int minutes,
        string sourceModule, string sourceId, string note, int? performedByEmpId, CancellationToken ct = default);

    /// <summary>
    /// Libera (devuelve) una reserva previa por su sourceId — reemplaza a
    /// HR.sp_hr_ReleaseReservation. Idempotente: si ya fue liberada retorna null sin error;
    /// si ya fue consumida (aprobada), lanza BusinessRuleException (no se libera lo aprobado).
    /// </summary>
    Task<VacationBalanceAdjustmentResultDto?> ReleaseReservationAsync(
        string reserveSourceId, int? performedByEmpId, CancellationToken ct = default);

    /// <summary>
    /// Marca una reserva como consumida (aprobada) — reemplaza a HR.sp_hr_ConsumeReservation.
    /// Es solo un marcador de auditoría: el saldo ya se descontó al reservar, esto no vuelve
    /// a tocar TimeBalances. Idempotente: si ya estaba consumida, no hace nada.
    /// </summary>
    Task MarkReservationConsumedAsync(string reserveSourceId, int? performedByEmpId, CancellationToken ct = default);

    /// <summary>
    /// Saldo actual de un empleado en su régimen laboral principal — el MISMO que resuelve
    /// <see cref="ReserveAsync"/> internamente. Existe para que un pre-check (ej. en
    /// VacationsService, antes de crear la fila) valide contra el régimen correcto en vez
    /// de resolverlo por otro camino que podría no coincidir (hueco cerrado 2026-07-22: el
    /// pre-check usaba "el régimen con el ID más bajo" vía ITimeBalancesRepository,
    /// mientras que Reserve usa "el régimen principal" — para un empleado con un solo
    /// régimen no hay diferencia, pero para uno con 2+ regímenes simultáneos donde el
    /// principal no es el de ID más bajo, podían no coincidir).
    /// </summary>
    Task<int> GetEmployeePrincipalBalanceAsync(int employeeId, TimeBalanceField field, CancellationToken ct = default);
}

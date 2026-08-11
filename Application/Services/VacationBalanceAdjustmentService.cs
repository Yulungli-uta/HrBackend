using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.TimeBalances;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Ajuste manual de saldo de vacaciones (individual o en lote), 100% EF Core —
/// no usa stored procedures. Reservado para operaciones disparadas desde pantalla
/// (carga inicial de un régimen, correcciones puntuales), no para la acreditación
/// automática masiva mensual (esa vive en un SP, sp_hr_AccrueVacationBalance_CT,
/// para mantener el mismo patrón que LOSEP/LOES).
/// </summary>
public class VacationBalanceAdjustmentService : IVacationBalanceAdjustmentService
{
    private const string ContractTypeCategory = "CONTRACT_TYPE";

    private readonly AppDbContext _db;
    private readonly ILogger<VacationBalanceAdjustmentService> _logger;

    public VacationBalanceAdjustmentService(AppDbContext db, ILogger<VacationBalanceAdjustmentService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VacationBalanceAdjustmentResultDto> AdjustAsync(
        VacationBalanceAdjustmentDto dto, int? performedByEmpId, CancellationToken ct = default)
    {
        if (dto is null) throw new ArgumentNullException(nameof(dto));
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new BusinessRuleException("El motivo del ajuste es obligatorio.");

        var regimeId = await ResolveLaborRegimeIdAsync(dto.LaborRegimeName, ct);

        var employeeExists = await _db.Employees.AsNoTracking().AnyAsync(e => e.EmployeeId == dto.EmployeeId, ct);
        if (!employeeExists)
            throw new BusinessRuleException($"Empleado (ID:{dto.EmployeeId}) no existe.");

        var hasActiveRegime = await _db.EmployeeLaborRegimes.AsNoTracking()
            .AnyAsync(r => r.EmployeeId == dto.EmployeeId && r.LaborRegimeId == regimeId && r.IsActive, ct);
        if (!hasActiveRegime)
            throw new BusinessRuleException(
                $"El empleado (ID:{dto.EmployeeId}) no tiene el régimen '{dto.LaborRegimeName}' activo. " +
                "Verifique el contrato/acción de personal antes de ajustar su saldo.");

        var sourceId = $"MANUAL_ADJ|{dto.EmployeeId}|{regimeId}|{Guid.NewGuid():N}";
        var sourceModule = dto.BalanceField == TimeBalanceField.Recovery ? "MANUAL_ADJUSTMENT_RECOVERY" : "MANUAL_ADJUSTMENT";

        return await ApplyAdjustmentAsync(
            employeeId: dto.EmployeeId,
            regimeId: regimeId,
            field: dto.BalanceField,
            mode: dto.Mode,
            valueMinutes: dto.ValueMinutes,
            allowNegativeResult: dto.AllowNegativeResult,
            sourceModule: sourceModule,
            sourceId: sourceId,
            note: dto.Reason,
            performedByEmpId: performedByEmpId,
            ct: ct);
    }

    public async Task<List<VacationBalanceBulkAdjustmentRowResultDto>> BulkAdjustAsync(
        VacationBalanceBulkAdjustmentRequestDto request, int? performedByEmpId, CancellationToken ct = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.BatchTag))
            throw new BusinessRuleException("BatchTag es obligatorio para identificar el lote.");

        var results = new List<VacationBalanceBulkAdjustmentRowResultDto>();
        var sourceModule = $"BULK_LOAD_{request.BatchTag}";

        // Cache de resolución de régimen por nombre, evita resolver 242 veces el mismo nombre.
        var regimeCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Items)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (string.IsNullOrWhiteSpace(item.Cedula))
                    throw new BusinessRuleException("Fila sin cédula.");

                if (string.IsNullOrWhiteSpace(item.Reason))
                    throw new BusinessRuleException("El motivo del ajuste es obligatorio.");

                if (!regimeCache.TryGetValue(item.LaborRegimeName, out var regimeId))
                {
                    regimeId = await ResolveLaborRegimeIdAsync(item.LaborRegimeName, ct);
                    regimeCache[item.LaborRegimeName] = regimeId;
                }

                var cedula = item.Cedula.Trim();
                var employeeId = await _db.Employees.AsNoTracking()
                    .Where(e => e.People!.IdCard == cedula)
                    .Select(e => (int?)e.EmployeeId)
                    .FirstOrDefaultAsync(ct);

                if (employeeId is null)
                {
                    results.Add(new VacationBalanceBulkAdjustmentRowResultDto
                    {
                        Cedula = cedula,
                        Success = false,
                        Message = "Cédula no encontrada (sin persona/empleado asociado)."
                    });
                    continue;
                }

                var hasActiveRegime = await _db.EmployeeLaborRegimes.AsNoTracking()
                    .AnyAsync(r => r.EmployeeId == employeeId.Value && r.LaborRegimeId == regimeId && r.IsActive, ct);

                if (!hasActiveRegime)
                {
                    results.Add(new VacationBalanceBulkAdjustmentRowResultDto
                    {
                        Cedula = cedula,
                        Success = false,
                        Message = $"No tiene régimen '{item.LaborRegimeName}' activo — verificar contrato/adenda primero."
                    });
                    continue;
                }

                var sourceId = $"BULK|{request.BatchTag}|{cedula}";

                var adjustResult = await ApplyAdjustmentAsync(
                    employeeId: employeeId.Value,
                    regimeId: regimeId,
                    field: item.BalanceField,
                    mode: item.Mode,
                    valueMinutes: item.ValueMinutes,
                    allowNegativeResult: item.AllowNegativeResult,
                    sourceModule: sourceModule,
                    sourceId: sourceId,
                    note: item.Reason,
                    performedByEmpId: performedByEmpId,
                    ct: ct);

                results.Add(new VacationBalanceBulkAdjustmentRowResultDto
                {
                    Cedula = cedula,
                    Success = true,
                    Message = "Cargado correctamente.",
                    PreviousBalanceMin = adjustResult.PreviousBalanceMin,
                    NewBalanceMin = adjustResult.NewBalanceMin
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BulkAdjust: fila con error. Cedula={Cedula}", item.Cedula);
                results.Add(new VacationBalanceBulkAdjustmentRowResultDto
                {
                    Cedula = item.Cedula ?? "(sin cédula)",
                    Success = false,
                    Message = ex.Message
                });
            }
            finally
            {
                // Evita que cambios trackeados (ej. tras un DbUpdateConcurrencyException) de esta
                // fila se arrastren y se guarden por error junto con la siguiente fila del lote.
                _db.ChangeTracker.Clear();
            }
        }

        return results;
    }

    private const string SettlementSourceModule = "VACATION_SETTLEMENT";

    public async Task<List<PendingVacationSettlementDto>> GetPendingSettlementsAsync(CancellationToken ct = default)
    {
        // Regímenes cerrados (IsActive=false, ya con EffectiveTo) que todavía no tienen
        // ningún movimiento de liquidación registrado.
        var closedRegimes = await _db.EmployeeLaborRegimes.AsNoTracking()
            .Where(r => !r.IsActive
                     && !_db.TimeBalanceMovements.Any(m =>
                            m.EmployeeID == r.EmployeeId
                         && m.LaborRegimeId == r.LaborRegimeId
                         && m.SourceModule == SettlementSourceModule))
            .Select(r => new { r.EmployeeId, r.LaborRegimeId, r.EffectiveTo, r.SourceContractId, r.SourcePersonnelActionId })
            .ToListAsync(ct);

        if (closedRegimes.Count == 0) return [];

        var employeeIds = closedRegimes.Select(r => r.EmployeeId).Distinct().ToList();
        var regimeIds = closedRegimes.Select(r => r.LaborRegimeId).Distinct().ToList();
        var contractIds = closedRegimes.Where(r => r.SourceContractId.HasValue).Select(r => r.SourceContractId!.Value).Distinct().ToList();
        var personnelActionIds = closedRegimes.Where(r => r.SourcePersonnelActionId.HasValue).Select(r => r.SourcePersonnelActionId!.Value).Distinct().ToList();

        var balances = await _db.TimeBalances.AsNoTracking()
            .Where(t => employeeIds.Contains(t.EmployeeID) && regimeIds.Contains(t.LaborRegimeId))
            .ToDictionaryAsync(t => (t.EmployeeID, t.LaborRegimeId), t => (t.VacationAvailableMin, t.RecoveryPendingMin), ct);

        var employeeNames = await _db.Set<WsUtaSystem.Models.Views.VwEmployeeDetails>().AsNoTracking()
            .Where(e => employeeIds.Contains(e.EmployeeID))
            .ToDictionaryAsync(e => e.EmployeeID, e => e.FirstName + " " + e.LastName, ct);

        var regimeNames = await _db.RefTypes.AsNoTracking()
            .Where(r => regimeIds.Contains(r.TypeId))
            .ToDictionaryAsync(r => r.TypeId, r => r.Name, ct);

        // Motivo del cierre — se resuelve por prioridad, sin crear endpoints nuevos por
        // escenario: renuncia/jubilación (vía ResignationRetirementRequest ligada a la
        // acción de personal), otra acción de personal cualquiera, estado del contrato
        // (RENUNCIA/VENCIDO), o "Cierre manual" si no calza con ninguno de los anteriores.
        var resignationRetirementByAction = personnelActionIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.ResignationRetirementRequests.AsNoTracking()
                .Where(rr => rr.LinkedPersonnelActionId.HasValue && personnelActionIds.Contains(rr.LinkedPersonnelActionId.Value))
                .ToDictionaryAsync(rr => rr.LinkedPersonnelActionId!.Value, rr => rr.RequestType, ct);

        var actionTypeNames = personnelActionIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.PersonnelActions.AsNoTracking()
                .Where(a => personnelActionIds.Contains(a.ActionId))
                .Join(_db.PersonnelActionTypes.AsNoTracking(), a => a.ActionTypeId, t => t.PersonnelActionTypeId, (a, t) => new { a.ActionId, t.Name })
                .ToDictionaryAsync(x => x.ActionId, x => x.Name, ct);

        var contractStatusNames = contractIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Contracts.AsNoTracking()
                .Where(c => contractIds.Contains(c.ContractID))
                .Join(_db.RefTypes.AsNoTracking(), c => c.Status, rt => rt.TypeId, (c, rt) => new { c.ContractID, rt.Name })
                .ToDictionaryAsync(x => x.ContractID, x => x.Name, ct);

        string ResolveTriggerReason(int? sourceContractId, int? sourcePersonnelActionId)
        {
            if (sourcePersonnelActionId.HasValue
                && resignationRetirementByAction.TryGetValue(sourcePersonnelActionId.Value, out var requestType))
                return requestType == DTOs.ResignationRetirement.ResignationRetirementRequestType.Retirement
                    ? "Jubilación" : "Renuncia";

            if (sourcePersonnelActionId.HasValue
                && actionTypeNames.TryGetValue(sourcePersonnelActionId.Value, out var actionTypeName))
                return $"Acción de personal: {actionTypeName}";

            if (sourceContractId.HasValue && contractStatusNames.TryGetValue(sourceContractId.Value, out var statusName))
            {
                return statusName switch
                {
                    "RENUNCIA" => "Renuncia",
                    "VENCIDO" => "Fin de contrato",
                    _ => $"Contrato: {statusName}"
                };
            }

            return "Cierre manual";
        }

        return closedRegimes
            .Select(r =>
            {
                balances.TryGetValue((r.EmployeeId, r.LaborRegimeId), out var balance);
                employeeNames.TryGetValue(r.EmployeeId, out var empName);
                regimeNames.TryGetValue(r.LaborRegimeId, out var regimeName);

                return new PendingVacationSettlementDto
                {
                    EmployeeId = r.EmployeeId,
                    EmployeeName = empName ?? $"Empleado #{r.EmployeeId}",
                    LaborRegimeId = r.LaborRegimeId,
                    LaborRegimeName = regimeName ?? $"Régimen #{r.LaborRegimeId}",
                    RegimeEffectiveTo = r.EffectiveTo,
                    CurrentBalanceMin = balance.VacationAvailableMin,
                    CurrentRecoveryBalanceMin = balance.RecoveryPendingMin,
                    TriggerReason = ResolveTriggerReason(r.SourceContractId, r.SourcePersonnelActionId)
                };
            })
            // Aparece en el buzón si queda CUALQUIER saldo sin liquidar — vacaciones o
            // recuperación de horas (a favor o en contra del empleado).
            .Where(d => d.CurrentBalanceMin != 0 || d.CurrentRecoveryBalanceMin != 0)
            .OrderBy(d => d.RegimeEffectiveTo)
            .ToList();
    }

    public async Task<VacationSettlementResultDto> SettleAsync(
        VacationSettlementRequestDto request, int? performedByEmpId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BusinessRuleException("El motivo de la liquidación es obligatorio.");

        var regimeId = await ResolveLaborRegimeIdAsync(request.LaborRegimeName, ct);

        var isClosed = await _db.EmployeeLaborRegimes.AsNoTracking()
            .AnyAsync(r => r.EmployeeId == request.EmployeeId && r.LaborRegimeId == regimeId && !r.IsActive, ct);

        if (!isClosed)
            throw new BusinessRuleException("Este régimen no está cerrado — la liquidación solo aplica a relaciones ya terminadas.");

        var alreadySettled = await _db.TimeBalanceMovements.AsNoTracking()
            .AnyAsync(m => m.EmployeeID == request.EmployeeId && m.LaborRegimeId == regimeId && m.SourceModule == SettlementSourceModule, ct);

        if (alreadySettled)
            throw new BusinessRuleException("Este régimen ya fue liquidado anteriormente.");

        // Ambas bolsas (vacaciones y recuperación de horas) se liquidan juntas en una sola
        // transacción — si alguna fallara (ej. conflicto de concurrencia agotando
        // reintentos), ninguna debe quedar aplicada a medias.
        var strategy = _db.Database.CreateExecutionStrategy();
        VacationSettlementResultDto result = null!;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            // El saldo puede quedar negativo hoy (deuda real de la persona) — fijar en 0 es
            // válido viniendo de positivo o de negativo, AllowNegativeResult no aplica porque
            // el destino (0) nunca es negativo.
            var vacation = await ApplyAdjustmentAsync(
                employeeId: request.EmployeeId,
                regimeId: regimeId,
                field: TimeBalanceField.Vacation,
                mode: VacationBalanceAdjustmentMode.Set,
                valueMinutes: 0,
                allowNegativeResult: false,
                sourceModule: SettlementSourceModule,
                sourceId: $"SETTLEMENT|{request.EmployeeId}|{regimeId}|VAC",
                note: request.Reason,
                performedByEmpId: performedByEmpId,
                ct: ct);

            var recovery = await ApplyAdjustmentAsync(
                employeeId: request.EmployeeId,
                regimeId: regimeId,
                field: TimeBalanceField.Recovery,
                mode: VacationBalanceAdjustmentMode.Set,
                valueMinutes: 0,
                allowNegativeResult: false,
                sourceModule: SettlementSourceModule,
                sourceId: $"SETTLEMENT|{request.EmployeeId}|{regimeId}|REC",
                note: request.Reason,
                performedByEmpId: performedByEmpId,
                ct: ct);

            await tx.CommitAsync(ct);

            result = new VacationSettlementResultDto
            {
                EmployeeId = request.EmployeeId,
                LaborRegimeId = regimeId,
                PreviousVacationBalanceMin = vacation.PreviousBalanceMin,
                NewVacationBalanceMin = vacation.NewBalanceMin,
                PreviousRecoveryBalanceMin = recovery.PreviousBalanceMin,
                NewRecoveryBalanceMin = recovery.NewBalanceMin
            };
        });

        return result;
    }

    public async Task<CurrentTimeBalanceDto> GetCurrentBalanceAsync(int employeeId, string laborRegimeName, CancellationToken ct = default)
    {
        var regimeId = await ResolveLaborRegimeIdAsync(laborRegimeName, ct);

        var balance = await _db.TimeBalances.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EmployeeID == employeeId && t.LaborRegimeId == regimeId, ct);

        return new CurrentTimeBalanceDto
        {
            EmployeeId = employeeId,
            LaborRegimeId = regimeId,
            LaborRegimeName = laborRegimeName,
            VacationAvailableMin = balance?.VacationAvailableMin ?? 0,
            RecoveryPendingMin = balance?.RecoveryPendingMin ?? 0
        };
    }

    /// <inheritdoc/>
    public async Task<VacationBalanceAdjustmentResultDto> ReserveAsync(
        int employeeId, TimeBalanceField field, int minutes,
        string sourceModule, string sourceId, string note, int? performedByEmpId, CancellationToken ct = default)
    {
        if (minutes <= 0)
            throw new BusinessRuleException("Los minutos a reservar deben ser mayores a 0.");

        var alreadyExists = await _db.TimeBalanceMovements.AsNoTracking()
            .AnyAsync(m => m.EmployeeID == employeeId && m.SourceID == sourceId, ct);

        if (alreadyExists)
            throw new BusinessRuleException($"Ya existe una reserva con este identificador: {sourceId}");

        var regimeId = await ResolveEmployeePrincipalRegimeIdAsync(employeeId, ct);

        return await ApplyAdjustmentAsync(
            employeeId: employeeId,
            regimeId: regimeId,
            field: field,
            mode: VacationBalanceAdjustmentMode.Increment,
            valueMinutes: -minutes,
            allowNegativeResult: false, // reservar SIEMPRE bloquea si no alcanza el saldo
            sourceModule: sourceModule,
            sourceId: sourceId,
            note: note,
            performedByEmpId: performedByEmpId,
            ct: ct);
    }

    /// <inheritdoc/>
    public async Task<VacationBalanceAdjustmentResultDto?> ReleaseReservationAsync(
        string reserveSourceId, int? performedByEmpId, CancellationToken ct = default)
    {
        var reservation = await _db.TimeBalanceMovements.AsNoTracking()
            .FirstOrDefaultAsync(m => m.SourceID == reserveSourceId, ct)
            ?? throw new BusinessRuleException($"Reserva no encontrada: {reserveSourceId}");

        var releaseSourceId = $"{reserveSourceId}|REL";

        var alreadyReleased = await _db.TimeBalanceMovements.AsNoTracking()
            .AnyAsync(m => m.SourceID == releaseSourceId, ct);
        if (alreadyReleased) return null; // idempotente, mismo criterio que la SP original

        var consumeSourceId = $"{reserveSourceId}|USE";
        var alreadyConsumed = await _db.TimeBalanceMovements.AsNoTracking()
            .AnyAsync(m => m.SourceID == consumeSourceId, ct);
        if (alreadyConsumed)
            throw new BusinessRuleException($"Esta reserva ya fue consumida (aprobada), no se puede liberar: {reserveSourceId}");

        var field = reservation.DeltaVacationMin != 0 ? TimeBalanceField.Vacation : TimeBalanceField.Recovery;
        var releaseAmount = field == TimeBalanceField.Vacation
            ? -reservation.DeltaVacationMin
            : -reservation.DeltaRecoveryMin;

        if (releaseAmount <= 0)
            throw new BusinessRuleException($"Reserva inválida (el delta original no es negativo) para: {reserveSourceId}");

        return await ApplyAdjustmentAsync(
            employeeId: reservation.EmployeeID,
            regimeId: reservation.LaborRegimeId
                ?? throw new BusinessRuleException($"La reserva {reserveSourceId} no tiene régimen asociado."),
            field: field,
            mode: VacationBalanceAdjustmentMode.Increment,
            valueMinutes: releaseAmount,
            allowNegativeResult: true, // liberar siempre suma — nunca puede empeorar un negativo
            sourceModule: "RESERVATION_RELEASE",
            sourceId: releaseSourceId,
            note: $"Liberación de reserva: {reserveSourceId}",
            performedByEmpId: performedByEmpId,
            ct: ct);
    }

    /// <inheritdoc/>
    public async Task MarkReservationConsumedAsync(string reserveSourceId, int? performedByEmpId, CancellationToken ct = default)
    {
        var reservation = await _db.TimeBalanceMovements.AsNoTracking()
            .FirstOrDefaultAsync(m => m.SourceID == reserveSourceId, ct)
            ?? throw new BusinessRuleException($"Reserva no encontrada: {reserveSourceId}");

        var consumeSourceId = $"{reserveSourceId}|USE";
        var alreadyConsumed = await _db.TimeBalanceMovements.AsNoTracking()
            .AnyAsync(m => m.SourceID == consumeSourceId, ct);
        if (alreadyConsumed) return; // idempotente

        // Hueco cerrado 2026-07-22 (no existía ni en la SP original ni en la primera versión
        // EF): no se puede consumir/aprobar una reserva que ya fue liberada/cancelada — el
        // saldo de esa reserva ya se devolvió, así que "aprobarla" ahora dejaría el registro
        // de auditoría diciendo que se usó un saldo que en realidad ya no estaba reservado.
        var releaseSourceId = $"{reserveSourceId}|REL";
        var alreadyReleased = await _db.TimeBalanceMovements.AsNoTracking()
            .AnyAsync(m => m.SourceID == releaseSourceId, ct);
        if (alreadyReleased)
            throw new BusinessRuleException(
                $"Esta reserva ya fue liberada/cancelada, no se puede consumir/aprobar: {reserveSourceId}");

        // Solo marcador de auditoría — el saldo ya se descontó al reservar (mismo criterio
        // que HR.sp_hr_ConsumeReservation: Delta=0, no vuelve a tocar TimeBalances).
        _db.TimeBalanceMovements.Add(new TimeBalanceMovements
        {
            EmployeeID = reservation.EmployeeID,
            DeltaVacationMin = 0,
            DeltaRecoveryMin = 0,
            MovementAt = DateTime.Now,
            SourceModule = "RESERVATION_CONSUME",
            SourceTable = "SYSTEM",
            SourceID = consumeSourceId,
            PerformedByEmpID = performedByEmpId,
            Note = $"Consumo (aprobación) de reserva: {reserveSourceId}",
            LaborRegimeId = reservation.LaborRegimeId
        });

        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<int> GetEmployeePrincipalBalanceAsync(int employeeId, TimeBalanceField field, CancellationToken ct = default)
    {
        var regimeId = await ResolveEmployeePrincipalRegimeIdAsync(employeeId, ct);

        var balance = await _db.TimeBalances.AsNoTracking()
            .FirstOrDefaultAsync(t => t.EmployeeID == employeeId && t.LaborRegimeId == regimeId, ct);

        return field == TimeBalanceField.Recovery
            ? balance?.RecoveryPendingMin ?? 0
            : balance?.VacationAvailableMin ?? 0;
    }

    /// <summary>
    /// Resuelve el régimen laboral activo al que aplica una reserva de vacaciones/permisos:
    /// el principal si el empleado tiene más de uno simultáneo, o el único activo que tenga.
    /// Reemplaza el "LOSEP siempre (57)" hardcodeado de las SP de reserva originales, que no
    /// funcionaba para empleados de Código de Trabajo/LOES (confirmado con prueba real:
    /// la reserva "tenía éxito" pero nunca descontaba su saldo real).
    /// </summary>
    private async Task<int> ResolveEmployeePrincipalRegimeIdAsync(int employeeId, CancellationToken ct)
    {
        var regimeId = await _db.EmployeeLaborRegimes.AsNoTracking()
            .Where(r => r.EmployeeId == employeeId && r.IsActive)
            .OrderByDescending(r => r.IsPrincipal)
            .ThenBy(r => r.EffectiveFrom)
            .Select(r => (int?)r.LaborRegimeId)
            .FirstOrDefaultAsync(ct);

        return regimeId
            ?? throw new BusinessRuleException(
                $"El empleado (ID:{employeeId}) no tiene ningún régimen laboral activo — no se puede reservar saldo.");
    }

    private async Task<int> ResolveLaborRegimeIdAsync(string name, CancellationToken ct)
    {
        var regimeId = await _db.RefTypes.AsNoTracking()
            .Where(r => r.Category == ContractTypeCategory && r.Name == name && r.IsActive)
            .Select(r => (int?)r.TypeId)
            .FirstOrDefaultAsync(ct);

        if (regimeId is null or 0)
            throw new BusinessRuleException($"Régimen laboral '{name}' no existe en el catálogo (ref_Types.CONTRACT_TYPE).");

        return regimeId.Value;
    }

    /// <summary>
    /// Punto de entrada transaccional compartido por el ajuste individual, cada fila del
    /// lote, y ahora también Reserve/Release (llamados desde VacationsService/
    /// PermissionsService, que YA tienen su propia transacción externa abierta sobre el
    /// mismo DbContext — igual que las SP originales distinguían "@@TRANCOUNT = 0 → BEGIN
    /// TRAN" de "@@TRANCOUNT > 0 → SAVE TRAN"):
    /// - Si NO hay transacción ambiente (uso standalone: ajuste manual, lote): abre y
    ///   confirma su propia transacción con estrategia de reintento.
    /// - Si YA hay una transacción ambiente (Reserve/Release llamados dentro de
    ///   CreateWithBalanceCheckAsync/UpdateBalanceAffectAsync): participa en ella sin abrir
    ///   ni confirmar nada — el commit/rollback final lo decide quien la inició, para que
    ///   la fila de Vacations/Permissions y el movimiento de saldo vivan o mueran juntos.
    /// </summary>
    private async Task<VacationBalanceAdjustmentResultDto> ApplyAdjustmentAsync(
        int employeeId,
        int regimeId,
        TimeBalanceField field,
        VacationBalanceAdjustmentMode mode,
        int valueMinutes,
        bool allowNegativeResult,
        string sourceModule,
        string sourceId,
        string note,
        int? performedByEmpId,
        CancellationToken ct)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await ApplyAdjustmentCoreAsync(
                employeeId, regimeId, field, mode, valueMinutes, allowNegativeResult,
                sourceModule, sourceId, note, performedByEmpId, ct);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        VacationBalanceAdjustmentResultDto? result = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            result = await ApplyAdjustmentCoreAsync(
                employeeId, regimeId, field, mode, valueMinutes, allowNegativeResult,
                sourceModule, sourceId, note, performedByEmpId, ct);

            await tx.CommitAsync(ct);
        });

        return result!;
    }

    /// <summary>
    /// Máximo de reintentos ante conflicto de concurrencia (RowVersion) antes de rendirse.
    /// Reemplaza el bloqueo pesimista (WITH UPDLOCK, HOLDLOCK) de las SP originales, que
    /// serializaba solicitudes concurrentes en silencio — con concurrencia optimista, la
    /// que "pierde la carrera" debe recargar el saldo ya actualizado y reintentar en vez de
    /// fallarle al usuario por una simple coincidencia de tiempo (2-3 reintentos alcanzan
    /// para el caso real: doble clic o dos pestañas del mismo empleado, nunca alta
    /// concurrencia genuina sobre el mismo saldo).
    /// </summary>
    private const int MaxConcurrencyRetries = 3;

    private async Task<VacationBalanceAdjustmentResultDto> ApplyAdjustmentCoreAsync(
        int employeeId,
        int regimeId,
        TimeBalanceField field,
        VacationBalanceAdjustmentMode mode,
        int valueMinutes,
        bool allowNegativeResult,
        string sourceModule,
        string sourceId,
        string note,
        int? performedByEmpId,
        CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            var balance = await _db.TimeBalances
                .FirstOrDefaultAsync(t => t.EmployeeID == employeeId && t.LaborRegimeId == regimeId, ct);

            var previousBalance = field == TimeBalanceField.Recovery
                ? balance?.RecoveryPendingMin ?? 0
                : balance?.VacationAvailableMin ?? 0;

            int newBalance;
            int delta;

            if (mode == VacationBalanceAdjustmentMode.Set)
            {
                newBalance = valueMinutes;
                delta = newBalance - previousBalance;
            }
            else
            {
                delta = valueMinutes;
                newBalance = previousBalance + delta;
            }

            if (newBalance < 0 && !allowNegativeResult)
                throw new BusinessRuleException(
                    $"El ajuste dejaría el saldo en {newBalance} min (negativo). " +
                    "Marque 'permitir negativo' si es intencional y documente el motivo.");

            if (balance is null)
            {
                balance = new TimeBalances
                {
                    EmployeeID = employeeId,
                    LaborRegimeId = regimeId,
                    VacationAvailableMin = field == TimeBalanceField.Vacation ? newBalance : 0,
                    RecoveryPendingMin = field == TimeBalanceField.Recovery ? newBalance : 0,
                    LastUpdated = DateTime.Now
                };
                _db.TimeBalances.Add(balance);
            }
            else
            {
                if (field == TimeBalanceField.Recovery)
                    balance.RecoveryPendingMin = newBalance;
                else
                    balance.VacationAvailableMin = newBalance;

                balance.LastUpdated = DateTime.Now;
            }

            _db.TimeBalanceMovements.Add(new TimeBalanceMovements
            {
                EmployeeID = employeeId,
                DeltaVacationMin = field == TimeBalanceField.Vacation ? delta : 0,
                DeltaRecoveryMin = field == TimeBalanceField.Recovery ? delta : 0,
                MovementAt = DateTime.Now,
                SourceModule = sourceModule,
                SourceTable = "MANUAL",
                SourceID = sourceId,
                PerformedByEmpID = performedByEmpId,
                Note = note,
                LaborRegimeId = regimeId
            });

            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Alguien más (otra solicitud concurrente del mismo empleado+régimen) guardó
                // primero. Se descarta el intento actual (ChangeTracker.Clear) y se recarga
                // el saldo ya actualizado para recalcular sobre el valor real más reciente —
                // mismo efecto práctico que el UPDLOCK/HOLDLOCK de la SP original, sin
                // bloquear físicamente la fila mientras se espera.
                _db.ChangeTracker.Clear();

                if (attempt >= MaxConcurrencyRetries)
                {
                    _logger.LogWarning(ex,
                        "Conflicto de concurrencia ajustando saldo tras {Attempts} intentos. EmployeeId={EmployeeId} RegimeId={RegimeId}",
                        attempt, employeeId, regimeId);
                    throw new BusinessRuleException(
                        "El saldo fue modificado por otro proceso mientras se procesaba este ajuste. Intente nuevamente.");
                }

                _logger.LogInformation(
                    "Conflicto de concurrencia ajustando saldo, reintentando (intento {Attempt}/{Max}). EmployeeId={EmployeeId} RegimeId={RegimeId}",
                    attempt, MaxConcurrencyRetries, employeeId, regimeId);
                continue;
            }

            return new VacationBalanceAdjustmentResultDto
            {
                EmployeeId = employeeId,
                LaborRegimeId = regimeId,
                PreviousBalanceMin = previousBalance,
                NewBalanceMin = newBalance,
                DeltaAppliedMin = delta
            };
        }
    }
}

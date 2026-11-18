using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.EmployeeLaborRegime;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Gestiona los regímenes laborales (LOSEP/LOES/CT) de un empleado, permitiendo
/// más de uno activo simultáneo (ej. nombramiento LOSEP + contrato ocasional LOES).
/// Calcula automáticamente cuál de los activos es el principal.
/// </summary>
public class EmployeeLaborRegimeService : IEmployeeLaborRegimeService
{
    private const string ContractTypeCategory = "CONTRACT_TYPE";
    private const string LosepName = "LOSEP";

    private readonly IEmployeeLaborRegimeRepository _repository;
    private readonly AppDbContext _db;

    private readonly IAppClock _clock;

    public EmployeeLaborRegimeService(IEmployeeLaborRegimeRepository repository, AppDbContext db, IAppClock clock)
    {
        _clock = clock;
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<List<EmployeeLaborRegimeDto>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
    {
        var items = await _repository.GetAllByEmployeeAsync(employeeId, ct);
        return await ToDtosAsync(items, ct);
    }

    public Task<List<int>> GetActiveRegimeIdsForEmployeesAsync(List<int> employeeIds, CancellationToken ct = default)
        => _repository.GetActiveRegimeIdsByEmployeeIdsAsync(employeeIds, ct);

    public async Task<EmployeeLaborRegimeDto> CreateAsync(EmployeeLaborRegimeCreateDto dto, int? changedBy, CancellationToken ct = default)
    {
        var alreadyActive = await _db.EmployeeLaborRegimes
            .AnyAsync(r => r.EmployeeId == dto.EmployeeId && r.LaborRegimeId == dto.LaborRegimeId && r.IsActive, ct);

        if (alreadyActive)
            throw new InvalidOperationException("El empleado ya tiene ese régimen activo.");

        ValidateDocumentSource(dto);

        var entity = new EmployeeLaborRegime
        {
            EmployeeId = dto.EmployeeId,
            LaborRegimeId = dto.LaborRegimeId,
            DepartmentId = dto.DepartmentId,
            JobId = dto.JobId,
            IsIndefinite = dto.IsIndefinite,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            SourceContractId = dto.SourceContractId,
            SourcePersonnelActionId = dto.SourcePersonnelActionId,
            EffectiveFrom = dto.EffectiveFrom,
            IsActive = true,
            IngresoPorConcurso = dto.IngresoPorConcurso,
            SiiesRelacionIesTypeId = await ValidateSiiesRelacionIesAsync(dto.SiiesRelacionIesTypeId, ct),
            CreatedBy = changedBy,
            CreatedAt = _clock.Now,
        };

        _db.EmployeeLaborRegimes.Add(entity);
        await _db.SaveChangesAsync(ct);

        await RecalculatePrincipalAsync(dto.EmployeeId, changedBy, ct);

        return (await ToDtosAsync([entity], ct)).Single();
    }

    /// <summary>Nunca crear un régimen "huérfano": exige un documento de respaldo real (contrato o acción) con su Id.</summary>
    private static void ValidateDocumentSource(EmployeeLaborRegimeCreateDto dto)
    {
        switch (dto.DocumentType)
        {
            case "CONTRACT":
                if (!dto.SourceContractId.HasValue)
                    throw new BusinessRuleException("DocumentType='CONTRACT' requiere SourceContractId.");
                break;
            case "PERSONNEL_ACTION":
                if (!dto.SourcePersonnelActionId.HasValue)
                    throw new BusinessRuleException("DocumentType='PERSONNEL_ACTION' requiere SourcePersonnelActionId.");
                break;
            default:
                throw new BusinessRuleException("DocumentType debe ser 'CONTRACT' o 'PERSONNEL_ACTION', con su Id de origen correspondiente.");
        }
    }

    /// <inheritdoc />
    public Task<bool> HasActiveContractRegimeAsync(int employeeId, int laborRegimeId, CancellationToken ct = default) =>
        _db.EmployeeLaborRegimes.AsNoTracking().AnyAsync(r => r.EmployeeId == employeeId
            && r.LaborRegimeId == laborRegimeId && r.IsActive && r.DocumentType == "CONTRACT", ct);

    /// <inheritdoc />
    public async Task<EmployeeLaborRegimeDto> RegisterDocumentAsync(EmployeeLaborRegimeCreateDto dto, int? changedBy, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateDocumentSource(dto);

        // Idempotente: un documento no se registra dos veces.
        var existing = await _db.EmployeeLaborRegimes.FirstOrDefaultAsync(r => r.EmployeeId == dto.EmployeeId
            && ((dto.SourceContractId != null && r.SourceContractId == dto.SourceContractId)
                || (dto.SourcePersonnelActionId != null && r.SourcePersonnelActionId == dto.SourcePersonnelActionId)), ct);
        if (existing is not null)
            return (await ToDtosAsync([existing], ct)).Single();

        var ownsTransaction = _db.Database.CurrentTransaction is null;
        var tx = ownsTransaction ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            var active = await _db.EmployeeLaborRegimes
                .Where(r => r.EmployeeId == dto.EmployeeId && r.LaborRegimeId == dto.LaborRegimeId && r.IsActive)
                .ToListAsync(ct);
            var isLatest = !active.Any(r => r.EffectiveFrom >= dto.EffectiveFrom);
            var endDate = dto.EffectiveTo;

            if (isLatest)
            {
                // El anterior se cierra el día antes de que empiece el nuevo (o con su propio fin, si ya lo tenía).
                foreach (var previous in active)
                {
                    previous.IsActive = false;
                    previous.IsPrincipal = false;
                    var close = previous.EffectiveTo ?? dto.EffectiveFrom.AddDays(-1);
                    previous.EffectiveTo = close < previous.EffectiveFrom ? previous.EffectiveFrom : close;
                    previous.UpdatedBy = changedBy;
                    previous.UpdatedAt = _clock.Now;
                }
                // El índice único (empleado, régimen, activo) exige cerrar antes de abrir.
                if (active.Count > 0) await _db.SaveChangesAsync(ct);
            }
            else
            {
                // Documento anterior al activo: queda cerrado hasta donde empieza el siguiente.
                var next = active.Where(r => r.EffectiveFrom >= dto.EffectiveFrom).OrderBy(r => r.EffectiveFrom).First();
                endDate ??= next.EffectiveFrom.AddDays(-1);
                if (endDate < dto.EffectiveFrom) endDate = dto.EffectiveFrom;
            }

            var entity = new EmployeeLaborRegime
            {
                EmployeeId = dto.EmployeeId,
                LaborRegimeId = dto.LaborRegimeId,
                DepartmentId = dto.DepartmentId,
                JobId = dto.JobId,
                IsIndefinite = dto.IsIndefinite,
                DocumentType = dto.DocumentType,
                DocumentNumber = dto.DocumentNumber,
                SourceContractId = dto.SourceContractId,
                SourcePersonnelActionId = dto.SourcePersonnelActionId,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = endDate,
                IsActive = isLatest,
                IngresoPorConcurso = dto.IngresoPorConcurso,
                SiiesRelacionIesTypeId = await ValidateSiiesRelacionIesAsync(dto.SiiesRelacionIesTypeId, ct),
                CreatedBy = changedBy,
                CreatedAt = _clock.Now,
            };
            _db.EmployeeLaborRegimes.Add(entity);
            await _db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);

            await RecalculatePrincipalAsync(dto.EmployeeId, changedBy, ct);
            return (await ToDtosAsync([entity], ct)).Single();
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    public async Task<EmployeeLaborRegimeDto?> CloseAsync(int id, EmployeeLaborRegimeCloseDto dto, int? changedBy, CancellationToken ct = default)
    {
        var entity = await _db.EmployeeLaborRegimes.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return null;

        entity.IsActive = false;
        entity.EffectiveTo = dto.EffectiveTo;
        entity.IsPrincipal = false;
        entity.UpdatedBy = changedBy;
        entity.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(ct);

        await RecalculatePrincipalAsync(entity.EmployeeId, changedBy, ct);

        return (await ToDtosAsync([entity], ct)).Single();
    }

    public async Task<EmployeeLaborRegimeDto?> SetSiiesRelacionIesAsync(int id, EmployeeLaborRegimeSiiesRelacionIesDto dto, int? changedBy, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var entity = await _db.EmployeeLaborRegimes.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return null;

        entity.SiiesRelacionIesTypeId = await ValidateSiiesRelacionIesAsync(dto.SiiesRelacionIesTypeId, ct);
        entity.UpdatedBy = changedBy;
        entity.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(ct);

        return (await ToDtosAsync([entity], ct)).Single();
    }

    /// <summary>Valida que el TypeId sea de la categoría SIIES_RELACION_IES (null se acepta: quita el valor manual).</summary>
    private async Task<int?> ValidateSiiesRelacionIesAsync(int? typeId, CancellationToken ct)
    {
        if (!typeId.HasValue) return null;

        var valid = await _db.RefTypes.AsNoTracking()
            .AnyAsync(t => t.TypeId == typeId.Value && t.Category == "SIIES_RELACION_IES", ct);
        if (!valid)
            throw new BusinessRuleException("La relación IES indicada no existe en el catálogo SIIES_RELACION_IES.");

        return typeId;
    }

    public async Task<EmployeeLaborRegimeDto?> SetIngresoPorConcursoAsync(int id, EmployeeLaborRegimeIngresoPorConcursoDto dto, int? changedBy, CancellationToken ct = default)
    {
        var entity = await _db.EmployeeLaborRegimes.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity is null) return null;

        entity.IngresoPorConcurso = dto.IngresoPorConcurso;
        entity.UpdatedBy = changedBy;
        entity.UpdatedAt = _clock.Now;

        await _db.SaveChangesAsync(ct);

        return (await ToDtosAsync([entity], ct)).Single();
    }

    private static readonly string[] EffectiveActionStatuses = ["VIGENTE", "FINALIZADO"];

    private static readonly string[] NonPositionCategories = PositionChangeRules.NonPositionCategories;

    /// <inheritdoc />
    public async Task<LaborRegimeSyncResult> SyncPositionFromPersonnelActionAsync(
        int actionId, int? changedBy, string? previousActionNumber = null, CancellationToken ct = default)
    {
        var action = await (
            from a in _db.PersonnelActions.AsNoTracking()
            where a.ActionId == actionId
            join t in _db.PersonnelActionTypes.AsNoTracking() on a.ActionTypeId equals t.PersonnelActionTypeId into tj
            from t in tj.DefaultIfEmpty()
            select new
            {
                a.EmployeeId, a.Status, a.EmployeeTypeId, a.ActionNumber, a.EffectiveDate, a.ActionDate,
                a.DestinationDepartmentId, a.DestinationJobId,
                Category = t == null ? null : t.ActionCategory,
                TypeCode = t == null ? null : t.Code
            }).FirstOrDefaultAsync(ct);

        if (action is null || !action.EmployeeId.HasValue)
            return new(LaborRegimeSyncOutcome.NotApplicable, null, "La acción no tiene empleado.");
        if (!EffectiveActionStatuses.Contains(action.Status, StringComparer.OrdinalIgnoreCase))
            return new(LaborRegimeSyncOutcome.NotApplicable, null, $"Estado '{action.Status}': no aplica.");
        if (!action.DestinationDepartmentId.HasValue && !action.DestinationJobId.HasValue)
            return new(LaborRegimeSyncOutcome.NotApplicable, null, "La acción no tiene departamento ni cargo de destino.");
        if (!PositionChangeRules.CanChangePosition(action.Category))
            return new(LaborRegimeSyncOutcome.NotApplicable, null,
                "El tipo de acción (licencia, vacaciones, dedicación, disciplinaria o salida) no cambia la dependencia ni el cargo.");

        var employeeId = action.EmployeeId.Value;
        var effectiveFrom = action.EffectiveDate ?? action.ActionDate;

        // Regla: la corrección afecta solo al registro que corresponde al documento. Primero el enlazado por Id; si no hay,
        // se busca uno suelto del mismo empleado cuyo número coincide (el actual o el anterior a la corrección) y se enlaza.
        var regime = await _db.EmployeeLaborRegimes
            .Where(r => r.SourcePersonnelActionId == actionId)
            .OrderByDescending(r => r.IsActive).ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);
        var linked = false;
        if (regime is null)
        {
            var numbers = new[] { action.ActionNumber, previousActionNumber }
                .Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!.Trim()).Distinct().ToList();
            if (numbers.Count > 0)
            {
                regime = await _db.EmployeeLaborRegimes
                    .Where(r => r.EmployeeId == employeeId && r.SourcePersonnelActionId == null && r.SourceContractId == null
                                && r.DocumentNumber != null && numbers.Contains(r.DocumentNumber))
                    .OrderByDescending(r => r.IsActive).ThenByDescending(r => r.Id)
                    .FirstOrDefaultAsync(ct);
                if (regime is not null)
                {
                    regime.SourcePersonnelActionId = actionId;
                    regime.DocumentType = "PERSONNEL_ACTION";
                    linked = true;
                }
            }
        }

        if (regime is not null)
        {
            var newDepartment = action.DestinationDepartmentId ?? regime.DepartmentId;
            var newJob = action.DestinationJobId ?? regime.JobId;
            var newNumber = string.IsNullOrWhiteSpace(action.ActionNumber) ? regime.DocumentNumber : action.ActionNumber;
            // La fecha de inicio sigue a la de la acción (igual que al crearlo), salvo que quede después de su fin.
            var newFrom = regime.EffectiveTo is { } to && effectiveFrom > to ? regime.EffectiveFrom : effectiveFrom;

            var changed = linked || regime.DepartmentId != newDepartment || regime.JobId != newJob
                          || regime.DocumentNumber != newNumber || regime.EffectiveFrom != newFrom;
            if (changed)
            {
                regime.DepartmentId = newDepartment;
                regime.JobId = newJob;
                regime.DocumentNumber = newNumber;
                regime.EffectiveFrom = newFrom;
                regime.UpdatedBy = changedBy;
                regime.UpdatedAt = _clock.Now;
                await _db.SaveChangesAsync(ct);
            }

            // Excepción: si es el último documento del empleado, hay que volver a determinar cuál es el nombramiento y el principal.
            var refreshed = await RefreshIfLatestAsync(regime, actionId, changedBy, ct);

            if (!changed && !refreshed)
                return new(LaborRegimeSyncOutcome.NoChange, regime.Id, "El régimen laboral ya coincide con la acción.");
            return new(LaborRegimeSyncOutcome.Updated, regime.Id,
                $"Régimen laboral #{regime.Id} {(linked ? "enlazado a la acción y " : string.Empty)}actualizado"
                + (refreshed ? " (último documento: quedó marcado como nombramiento)." : "."));
        }

        // Una Designación de quien ya tiene un contrato activo del mismo régimen no es una relación laboral nueva.
        if (action.EmployeeTypeId.HasValue && PositionChangeRules.IsConcurrentDesignation(
                action.TypeCode, await HasActiveContractRegimeAsync(employeeId, action.EmployeeTypeId.Value, ct)))
            return new(LaborRegimeSyncOutcome.NotApplicable, null,
                "Designación de quien ya tiene un contrato activo del mismo régimen: no es una relación laboral nueva.");

        // Sin registro: crear uno activo solo tiene sentido si esta es la acción más reciente del empleado; si hay un
        // movimiento posterior, el registro histórico de este documento lo resuelve la reconstrucción del historial.
        var hasLaterMovement = await _db.PersonnelActions.AsNoTracking()
            .AnyAsync(b => b.EmployeeId == employeeId
                && b.ActionId != actionId
                && (b.Status == "VIGENTE" || b.Status == "FINALIZADO")
                && (b.DestinationDepartmentId != null || b.DestinationJobId != null)
                && !_db.PersonnelActionTypes.Any(t => t.PersonnelActionTypeId == b.ActionTypeId
                    && t.ActionCategory != null && NonPositionCategories.Contains(t.ActionCategory))
                && ((b.EffectiveDate ?? b.ActionDate) > effectiveFrom
                    || ((b.EffectiveDate ?? b.ActionDate) == effectiveFrom && b.ActionId > actionId)), ct);
        if (hasLaterMovement)
            return new(LaborRegimeSyncOutcome.SkippedLaterMovement, null,
                "La acción no tiene registro en el régimen laboral y el empleado tiene una acción posterior: se registrará con la reconstrucción del historial.");
        if (!string.Equals(action.Status, "VIGENTE", StringComparison.OrdinalIgnoreCase))
            return new(LaborRegimeSyncOutcome.NotApplicable, null, "Acción finalizada sin régimen: no se crea uno nuevo.");
        if (!action.EmployeeTypeId.HasValue)
            return new(LaborRegimeSyncOutcome.SkippedNoRegimeType, null,
                "No se creó el régimen laboral: la acción no indica el tipo de empleado.");

        var employee = await _db.Employees.AsNoTracking()
            .Where(e => e.EmployeeId == employeeId)
            .Select(e => new { e.DepartmentId, e.JobId })
            .FirstOrDefaultAsync(ct);
        var changesDepartment = action.DestinationDepartmentId.HasValue && action.DestinationDepartmentId != employee?.DepartmentId;
        var changesJob = action.DestinationJobId.HasValue && action.DestinationJobId != employee?.JobId;
        if (!changesDepartment && !changesJob)
            return new(LaborRegimeSyncOutcome.NoChange, null, "La acción no cambia el departamento ni el cargo del empleado.");

        try
        {
            var created = await RegisterDocumentAsync(new EmployeeLaborRegimeCreateDto
            {
                EmployeeId = employeeId,
                LaborRegimeId = action.EmployeeTypeId.Value,
                DepartmentId = action.DestinationDepartmentId,
                JobId = action.DestinationJobId,
                IsIndefinite = false,
                DocumentType = "PERSONNEL_ACTION",
                DocumentNumber = action.ActionNumber,
                SourcePersonnelActionId = actionId,
                EffectiveFrom = effectiveFrom,
            }, changedBy, ct);

            var row = await _db.EmployeeLaborRegimes.FirstAsync(r => r.Id == created.Id, ct);
            await RefreshIfLatestAsync(row, actionId, changedBy, ct);
            return new(LaborRegimeSyncOutcome.Created, created.Id,
                $"Régimen laboral #{created.Id} creado por cambio de departamento o cargo.");
        }
        catch (InvalidOperationException ex)
        {
            return new(LaborRegimeSyncOutcome.SkippedActiveRegimeExists, null,
                $"No se creó el régimen laboral: {ex.Message} Revíselo manualmente en Funcionario Régimen Laboral.");
        }
    }

    /// <summary>
    /// Si el registro es el más reciente del empleado (mayor fecha de inicio), vuelve a determinar cuál es su nombramiento
    /// (según <c>HR.vw_EmployeeAppointment</c>: el documento con el que ganó el nombramiento) y recalcula el principal.
    /// Para un documento anterior no se hace nada: su corrección no afecta a los demás registros. Devuelve true si cambió la marca de nombramiento.
    /// </summary>
    private async Task<bool> RefreshIfLatestAsync(EmployeeLaborRegime regime, int? actionId, int? changedBy, CancellationToken ct)
    {
        var isLatest = !await _db.EmployeeLaborRegimes.AsNoTracking()
            .AnyAsync(o => o.EmployeeId == regime.EmployeeId && o.Id != regime.Id
                           && (o.EffectiveFrom > regime.EffectiveFrom
                               || (o.EffectiveFrom == regime.EffectiveFrom && o.Id > regime.Id)), ct);
        if (!isLatest) return false;

        var touched = false;
        if (actionId.HasValue)
        {
            var appointmentActionId = await _db.Database
                .SqlQuery<int?>($"SELECT AppointmentActionId AS [Value] FROM HR.vw_EmployeeAppointment WHERE EmployeeID = {regime.EmployeeId}")
                .FirstOrDefaultAsync(ct);
            if (appointmentActionId == actionId.Value && !regime.IsIndefinite)
            {
                regime.IsIndefinite = true;
                regime.UpdatedBy = changedBy;
                regime.UpdatedAt = _clock.Now;
                await _db.SaveChangesAsync(ct);
                touched = true;
            }
        }

        // El principal se recalcula siempre para el último documento; solo se avisa si cambió la marca de nombramiento.
        await RecalculatePrincipalAsync(regime.EmployeeId, changedBy, ct);
        return touched;
    }

    /// <inheritdoc />
    public async Task<LaborRegimeSyncResult> SyncFromContractAsync(int contractId, DateTime? previousEndDate, int? changedBy, string? previousContractCode = null, CancellationToken ct = default)
    {
        var contract = await _db.Contracts.AsNoTracking()
            .Where(c => c.ContractID == contractId)
            .Select(c => new { c.ContractCode, c.DepartmentID, c.JobID, c.StartDate, c.EndDate, c.Status, c.PersonID })
            .FirstOrDefaultAsync(ct);
        if (contract is null)
            return new(LaborRegimeSyncOutcome.NotApplicable, null, "El contrato no existe.");

        // Solo un contrato ya firmado/en efecto se replica en el régimen; borrador, generado, pendiente de firmas o anulado
        // todavía no (o ya no) respaldan un régimen. Los estados salen del catálogo por nombre, no por Id fijo.
        var effectiveStatusIds = await _db.RefTypes.AsNoTracking()
            .Where(r => r.Category == "CONTRACT_STATUS"
                        && (r.Name == "FIRMADO_CARGADO" || r.Name == "VIGENTE" || r.Name == "VENCIDO"
                            || r.Name == "RENUNCIA" || r.Name == "FINALIZADO"))
            .Select(r => r.TypeId)
            .ToListAsync(ct);
        if (!effectiveStatusIds.Contains(contract.Status))
            return new(LaborRegimeSyncOutcome.NotApplicable, null,
                "El contrato aún no está en un estado que respalde el régimen laboral (borrador, generado, pendiente de firmas o anulado).");

        var regime = await _db.EmployeeLaborRegimes
            .Where(r => r.SourceContractId == contractId)
            .OrderByDescending(r => r.IsActive).ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);
        var contractLinked = false;
        if (regime is null)
        {
            // Registro suelto del mismo empleado cuyo número coincide con el código actual o anterior del contrato: se enlaza.
            var codes = new[] { contract.ContractCode, previousContractCode }
                .Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!.Trim()).Distinct().ToList();
            if (codes.Count > 0)
            {
                regime = await _db.EmployeeLaborRegimes
                    .Where(r => r.SourceContractId == null && r.SourcePersonnelActionId == null
                                && r.DocumentNumber != null && codes.Contains(r.DocumentNumber)
                                && _db.Employees.Any(e => e.EmployeeId == r.EmployeeId && e.PersonID == contract.PersonID))
                    .OrderByDescending(r => r.IsActive).ThenByDescending(r => r.Id)
                    .FirstOrDefaultAsync(ct);
                if (regime is not null)
                {
                    regime.SourceContractId = contractId;
                    regime.DocumentType = "CONTRACT";
                    contractLinked = true;
                }
            }
        }
        if (regime is null)
            return new(LaborRegimeSyncOutcome.NotApplicable, null, "El contrato no tiene régimen laboral asociado.");

        var newDepartment = contract.DepartmentID > 0 ? contract.DepartmentID : regime.DepartmentId;
        var newJob = contract.JobID ?? regime.JobId;
        var newNumber = string.IsNullOrWhiteSpace(contract.ContractCode) ? regime.DocumentNumber : contract.ContractCode;
        var startDate = DateOnly.FromDateTime(contract.StartDate);
        var newFrom = regime.EffectiveTo is { } to && startDate > to ? regime.EffectiveFrom : startDate;

        // El fin del régimen sigue al del contrato solo si venía de él (coincidía con la fecha de fin anterior del contrato);
        // un cierre por renuncia u otro motivo no se pisa.
        var newTo = regime.EffectiveTo;
        if (previousEndDate.HasValue && regime.EffectiveTo == DateOnly.FromDateTime(previousEndDate.Value)
            && contract.EndDate.Year < 9999)
        {
            newTo = DateOnly.FromDateTime(contract.EndDate);
        }

        var contractChanged = contractLinked || regime.DepartmentId != newDepartment || regime.JobId != newJob
                              || regime.DocumentNumber != newNumber || regime.EffectiveFrom != newFrom || regime.EffectiveTo != newTo;
        if (contractChanged)
        {
            regime.DepartmentId = newDepartment;
            regime.JobId = newJob;
            regime.DocumentNumber = newNumber;
            regime.EffectiveFrom = newFrom;
            regime.EffectiveTo = newTo;
            regime.UpdatedBy = changedBy;
            regime.UpdatedAt = _clock.Now;
            await _db.SaveChangesAsync(ct);
        }

        // Excepción: si es el último documento del empleado, se recalcula cuál es el principal.
        var refreshedContract = await RefreshIfLatestAsync(regime, null, changedBy, ct);
        if (!contractChanged && !refreshedContract)
            return new(LaborRegimeSyncOutcome.NoChange, regime.Id, "El régimen laboral ya coincide con el contrato.");

        return new(LaborRegimeSyncOutcome.Updated, regime.Id,
            $"Régimen laboral #{regime.Id} {(contractLinked ? "enlazado al contrato y " : string.Empty)}actualizado"
            + (refreshedContract ? "." : "."));
    }

    /// <summary>
    /// Entre los regímenes activos del empleado: gana el nombramiento (IsIndefinite);
    /// si ninguno es nombramiento, gana LOSEP; si tampoco hay LOSEP activo, gana el más antiguo.
    /// </summary>
    private async Task RecalculatePrincipalAsync(int employeeId, int? changedBy, CancellationToken ct)
    {
        var active = await _db.EmployeeLaborRegimes
            .Where(r => r.EmployeeId == employeeId && r.IsActive)
            .ToListAsync(ct);

        if (active.Count == 0) return;

        EmployeeLaborRegime? principal = active
            .Where(r => r.IsIndefinite)
            .OrderBy(r => r.EffectiveFrom)
            .FirstOrDefault();

        if (principal is null)
        {
            var losepId = await _db.RefTypes
                .AsNoTracking()
                .Where(r => r.Category == ContractTypeCategory && r.Name == LosepName && r.IsActive)
                .Select(r => r.TypeId)
                .FirstOrDefaultAsync(ct);

            principal = (losepId != 0 ? active.Where(r => r.LaborRegimeId == losepId).OrderBy(r => r.EffectiveFrom).FirstOrDefault() : null)
                ?? active.OrderBy(r => r.EffectiveFrom).First();
        }

        foreach (var regime in active)
        {
            var shouldBePrincipal = regime.Id == principal.Id;
            if (regime.IsPrincipal == shouldBePrincipal) continue;

            regime.IsPrincipal = shouldBePrincipal;
            regime.UpdatedBy = changedBy;
            regime.UpdatedAt = _clock.Now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<EmployeeLaborRegimeDto>> ToDtosAsync(List<EmployeeLaborRegime> items, CancellationToken ct)
    {
        if (items.Count == 0) return [];

        var employeeIds = items.Select(i => i.EmployeeId).Distinct().ToList();
        var regimeIds = items.Select(i => i.LaborRegimeId).Distinct().ToList();
        var departmentIds = items.Where(i => i.DepartmentId.HasValue).Select(i => i.DepartmentId!.Value).Distinct().ToList();
        var jobIds = items.Where(i => i.JobId.HasValue).Select(i => i.JobId!.Value).Distinct().ToList();

        // vw_EmployeeDetailsAll (sin el filtro IsActive de vw_EmployeeDetails): un régimen de quien ya no trabaja
        // sigue necesitando nombre y correo en pantalla.
        var employees = await _db.Set<WsUtaSystem.Models.Views.VwEmployeeDetails>()
            .FromSqlRaw("SELECT * FROM HR.vw_EmployeeDetailsAll")
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.EmployeeID))
            .ToDictionaryAsync(e => e.EmployeeID, e => (Name: e.LastName + " " + e.FirstName, e.Email), ct);

        var regimeNames = await _db.RefTypes
            .AsNoTracking()
            .Where(r => regimeIds.Contains(r.TypeId))
            .ToDictionaryAsync(r => r.TypeId, r => r.Name, ct);

        var relationIds = items.Where(i => i.SiiesRelacionIesTypeId.HasValue).Select(i => i.SiiesRelacionIesTypeId!.Value).Distinct().ToList();
        var relationNames = relationIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.RefTypes.AsNoTracking()
                .Where(r => relationIds.Contains(r.TypeId))
                .ToDictionaryAsync(r => r.TypeId, r => r.Name, ct);

        var departmentNames = departmentIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Departments.AsNoTracking()
                .Where(d => departmentIds.Contains(d.DepartmentId))
                .ToDictionaryAsync(d => d.DepartmentId, d => d.Name, ct);

        var jobNames = jobIds.Count == 0
            ? new Dictionary<int, string?>()
            : await _db.Set<Job>().AsNoTracking()
                .Where(j => jobIds.Contains(j.JobID))
                .ToDictionaryAsync(j => j.JobID, j => j.Description, ct);

        return items.Select(i =>
        {
            employees.TryGetValue(i.EmployeeId, out var employee);
            regimeNames.TryGetValue(i.LaborRegimeId, out var regimeName);

            return new EmployeeLaborRegimeDto
            {
                Id = i.Id,
                EmployeeId = i.EmployeeId,
                EmployeeName = employee.Name,
                EmployeeEmail = employee.Email,
                LaborRegimeId = i.LaborRegimeId,
                LaborRegimeName = regimeName,
                DepartmentId = i.DepartmentId,
                DepartmentName = i.DepartmentId.HasValue && departmentNames.TryGetValue(i.DepartmentId.Value, out var dn) ? dn : null,
                JobId = i.JobId,
                JobName = i.JobId.HasValue && jobNames.TryGetValue(i.JobId.Value, out var jn) ? jn : null,
                IsIndefinite = i.IsIndefinite,
                DocumentType = i.DocumentType,
                DocumentNumber = i.DocumentNumber,
                SourceContractId = i.SourceContractId,
                SourcePersonnelActionId = i.SourcePersonnelActionId,
                EffectiveFrom = i.EffectiveFrom,
                EffectiveTo = i.EffectiveTo,
                IsActive = i.IsActive,
                IsPrincipal = i.IsPrincipal,
                IngresoPorConcurso = i.IngresoPorConcurso,
                SiiesRelacionIesTypeId = i.SiiesRelacionIesTypeId,
                SiiesRelacionIesName = i.SiiesRelacionIesTypeId.HasValue && relationNames.TryGetValue(i.SiiesRelacionIesTypeId.Value, out var rn) ? rn : null,
            };
        }).ToList();
    }
}

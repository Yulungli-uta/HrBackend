using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.TimeRecoveryPlans;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// OBSOLETO / SIN USO REAL (verificado 2026-07-22): este CRUD escribe en
/// HR.tbl_TimeRecoveryPlans, una tabla duplicada y huérfana. El mecanismo real de
/// planificación de recuperación de horas fuera de horario es
/// HR.tbl_TimePlanning (PlanType='Recovery'), expuesto por
/// <c>Controllers/HR/TimePlanningsController</c> / <c>TimePlanningEmployeesController</c>
/// (ruta <c>/api/v1/rh/planning/...</c>) y usado por el frontend
/// (components/planning/CreatePlanningDialog.tsx). Ese es el único camino que el
/// pipeline diario de asistencia (sp_ProcessAttendanceBaseDay/
/// sp_ProcessAttendancePlanningDay) cruza contra las picadas reales para descontar
/// HR.tbl_TimeBalances.RecoveryPendingMin automáticamente.
/// Confirmado sin llamadores: ningún Service/Controller/Job del backend invoca
/// <see cref="Application.Interfaces.Repositories.IHrBalanceRepository.ProcessRecoveryAsync"/>
/// ni <see cref="Application.Interfaces.Repositories.IHrBalanceRepository.DebitRecoveryAsync"/>
/// (las SP que sí decrementarían el saldo desde esta tabla), y ningún componente del
/// frontend usa <c>PlanesRecuperacionTiempoAPI</c>. No usar este controller para nada
/// nuevo — no tiene ningún efecto sobre el saldo real del empleado.
///
/// CORRECCIÓN 2026-07-22: solo este CRUD de escritura está muerto. La LECTURA de
/// HR.tbl_TimeRecoveryPlans/TimeRecoveryLogs SÍ está viva — HR.sp_ProcessAttendanceRecoveryDay
/// (etapa 4 del pipeline diario) las lee y perdona la marca de ausencia del día, algo que
/// TimePlanning NO hace (TimePlanning solo paga la deuda de RecoveryPendingMin). No borrar
/// las tablas ni asumir que están completamente huérfanas — falta construir un flujo de
/// creación real si "perdonar ausencia" sigue siendo una función deseada.
/// </summary>
[Obsolete("Solo este CRUD de escritura está sin uso real — el mecanismo vigente para PLANIFICAR recuperación es HR.tbl_TimePlanning (PlanType='Recovery'). OJO: la LECTURA de esta tabla (HR.sp_ProcessAttendanceRecoveryDay) sigue viva y perdona ausencias, no la borre. Ver comentario de clase.")]
[ApiController]
[Route("time-recovery/plans")]
public class TimeRecoveryPlansController : ControllerBase
{
    private readonly ITimeRecoveryPlansService _svc;
    private readonly IMapper _mapper;
    public TimeRecoveryPlansController(ITimeRecoveryPlansService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de TimeRecoveryPlans.</summary>
    [HttpGet]
    [RequirePermission("TIME_RECOVERY.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<TimeRecoveryPlansDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("TIME_RECOVERY.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<TimeRecoveryPlansDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("TIME_RECOVERY.CREATE")]
    public async Task<IActionResult> Create([FromBody] TimeRecoveryPlansCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<TimeRecoveryPlans>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<TimeRecoveryPlansDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("TIME_RECOVERY.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TimeRecoveryPlansUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<TimeRecoveryPlans>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("TIME_RECOVERY.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}

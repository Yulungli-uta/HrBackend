using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.TimeBalances;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
using WsUtaSystem.Application.DTOs.TimeBalances.TimeBalancesDTO;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("timebalances")]
public class TimeBalancesController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly ITimeBalancesService _svc;
    private readonly IVacationBalanceAdjustmentService _adjustmentSvc;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public TimeBalancesController(
        ITimeBalancesService svc,
        IVacationBalanceAdjustmentService adjustmentSvc,
        ICurrentUserService currentUser,
        IMapper mapper)
    {
        _svc = svc;
        _adjustmentSvc = adjustmentSvc;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    /// <summary>Ajuste manual individual de saldo de vacaciones (incrementar/descontar/establecer). Acción administrativa — no autoservicio.</summary>
    [HttpPost("adjust")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> Adjust([FromBody] VacationBalanceAdjustmentDto dto, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ajustar saldos de vacaciones.");

        try
        {
            var result = await _adjustmentSvc.AdjustAsync(dto, _currentUser.EmployeeId, ct);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { status = "error", message = ex.Message });
        }
    }

    /// <summary>Carga masiva de saldo de vacaciones (ej. listado de Código de Trabajo), fila por fila, con reporte individual — una fila con error no revierte las demás.</summary>
    [HttpPost("bulk-adjust")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> BulkAdjust([FromBody] VacationBalanceBulkAdjustmentRequestDto request, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para cargar saldos de vacaciones en lote.");

        var results = await _adjustmentSvc.BulkAdjustAsync(request, _currentUser.EmployeeId, ct);
        return Ok(results);
    }

    /// <summary>Buzón: empleados con régimen cerrado (contrato terminado) y saldo pendiente de liquidar.</summary>
    [HttpGet("pending-settlements")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetPendingSettlements(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver liquidaciones pendientes.");

        return Ok(await _adjustmentSvc.GetPendingSettlementsAsync(ct));
    }

    /// <summary>Saldo actual (vacaciones + recuperación) de un empleado en un régimen específico, para precargar la pantalla de ajuste.</summary>
    [HttpGet("current")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetCurrentBalance([FromQuery] int employeeId, [FromQuery] string laborRegimeName, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para consultar saldos de otros empleados.");

        try
        {
            return Ok(await _adjustmentSvc.GetCurrentBalanceAsync(employeeId, laborRegimeName, ct));
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { status = "error", message = ex.Message });
        }
    }

    /// <summary>Procesa la liquidación de un régimen ya cerrado — fija el saldo en 0 (puede venir de negativo) con motivo y auditoría obligatorios.</summary>
    [HttpPost("settle")]
    [RequirePermission("ATTENDANCE.MANAGE")]
    public async Task<IActionResult> Settle([FromBody] VacationSettlementRequestDto request, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para procesar liquidaciones.");

        try
        {
            var result = await _adjustmentSvc.SettleAsync(request, _currentUser.EmployeeId, ct);
            return Ok(result);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new { status = "error", message = ex.Message });
        }
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });

    /// <summary>Lista todos los registros de TimeBalances. Solo RRHH/administración — expone el saldo de todos los empleados.</summary>
    [HttpGet]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver los saldos de todos los empleados.");

        var entities = await _svc.GetAllAsync(ct);
        var dtos = _mapper.Map<List<TimeBalancesResponseDTO>>(entities);
        return Ok(dtos);
    }

    /// <summary>Obtiene un registro por EmployeeID. El propio empleado puede ver su saldo; para ver el de otro se requiere rol elevado.</summary>
    /// <param name="employeeId">Identificador del empleado</param>
    [HttpGet("{employeeId:int}")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetById([FromRoute] int employeeId, CancellationToken ct)
    {
        if (_currentUser.EmployeeId != employeeId && !ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No puede consultar el saldo de otro empleado.");

        var entity = await _svc.GetByIdAsync(employeeId, ct);
        return entity is null ?
            NotFound() :
            Ok(_mapper.Map<TimeBalancesResponseDTO>(entity));
    }

    /// <summary>
    /// Crea un nuevo registro de TimeBalances. Solo RRHH/administración — para cargar o
    /// ajustar saldo real usar POST /timebalances/adjust o /bulk-adjust (con motivo
    /// obligatorio y trazabilidad en TimeBalanceMovements); este endpoint es CRUD genérico
    /// sin auditoría y sin campo LaborRegimeId en el DTO (ver nota en Update).
    /// </summary>
    [HttpPost]
    [RequirePermission("ATTENDANCE.CREATE")]
    public async Task<IActionResult> Create([FromBody] TimeBalancesCreateDTO dto, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para crear registros de saldo.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dto.VacationAvailableMin < 0 || dto.RecoveryPendingMin < 0)
            return BadRequest(new { status = "error", message = "El saldo no puede crearse en negativo por esta vía. Use /timebalances/adjust con AllowNegativeResult si es intencional." });

        var entity = _mapper.Map<TimeBalances>(dto);
        var created = await _svc.CreateAsync(entity, ct);

        // En este caso, el ID es EmployeeID
        return CreatedAtAction(
            nameof(GetById),
            new { employeeId = created?.EmployeeID },
            _mapper.Map<TimeBalancesResponseDTO>(created)
        );
    }

    /// <summary>
    /// Actualiza un registro existente de TimeBalances. Solo RRHH/administración, nunca
    /// autoservicio. Sin auditoría (no escribe TimeBalanceMovements) y sin motivo obligatorio
    /// — para cualquier cambio real de saldo preferir POST /timebalances/adjust.
    /// NOTA: TimeBalancesUpdateDTO no incluye LaborRegimeId (la clave de tbl_TimeBalances es
    /// compuesta EmployeeID+LaborRegimeId) — si el empleado tiene más de un régimen activo,
    /// este endpoint no puede distinguir a cuál fila apunta de forma confiable.
    /// </summary>
    [HttpPut("{employeeId:int}")]
    [RequirePermission("ATTENDANCE.UPDATE")]
    public async Task<IActionResult> Update(
        [FromRoute] int employeeId,
        [FromBody] TimeBalancesUpdateDTO dto,
        CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para modificar saldos de vacaciones.");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (dto.VacationAvailableMin < 0 || dto.RecoveryPendingMin < 0)
            return BadRequest(new { status = "error", message = "El saldo no puede dejarse en negativo por esta vía. Use /timebalances/adjust con AllowNegativeResult si es intencional." });

        var entity = _mapper.Map<TimeBalances>(dto);
        entity.EmployeeID = employeeId; // Asegurar que el ID sea el correcto

        await _svc.UpdateAsync(employeeId, entity, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por EmployeeID. Solo RRHH/administración.</summary>
    [HttpDelete("{employeeId:int}")]
    [RequirePermission("ATTENDANCE.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int employeeId, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para eliminar registros de saldo.");

        await _svc.DeleteAsync(employeeId, ct);
        return NoContent();
    }

    /// <summary>Obtiene balances por múltiples empleados. Solo RRHH/administración.</summary>
    [HttpGet("by-employees")]
    [RequirePermission("ATTENDANCE.READ")]
    public async Task<IActionResult> GetByEmployeeIds(
        [FromQuery, Required] int[] employeeIds,
        CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para consultar saldos de otros empleados.");

        var entities = await _svc.GetAllAsync(ct);
        var filtered = entities.Where(e => employeeIds.Contains(e.EmployeeID)).ToList();
        return Ok(_mapper.Map<List<TimeBalancesResponseDTO>>(filtered));
    }
}
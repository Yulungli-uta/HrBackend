using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz.Util;
using WsUtaSystem.Application.DTOs.BankAccounts;
using WsUtaSystem.Application.DTOs.Jobs;
using WsUtaSystem.Application.DTOs.PunchJustifications;
using WsUtaSystem.Application.DTOs.RefTypes;
using WsUtaSystem.Application.DTOs.StoredProcedures;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/justifications")]
public class JustificationsController : ControllerBase
{
    private readonly IJustificationsService _svc;
    private readonly IMapper _mapper;

    public JustificationsController(IJustificationsService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<PunchJustificationsDto>>(await _svc.GetAllAsync(ct)));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<PunchJustificationsDto>(e));
    }

    [HttpGet("employeeid/{employeeID}")]
    public async Task<IActionResult> GetByEmployeeId([FromRoute] int employeeID, CancellationToken ct)
    {
        // Validación básica
        if (employeeID <1) { 
            return BadRequest("el EmployeeID no puede estar en 0");
        }

        var entities = await _svc.GetByEmployeeId(employeeID, ct);

        // Si no se encuentran resultados, devolver un 404
        if (!entities.Any())
        {
            return NotFound($"No se encontraron registros de justificacion para este EmployeeID '{employeeID}'");
        }

        return Ok(_mapper.Map<List<PunchJustificationsDto>>(entities));
    }

    [HttpGet("bossId/{BossEmployeeId:int}")]
    public async Task<IActionResult> GetByBossEmployeeId([FromRoute] int BossEmployeeId, CancellationToken ct)
    {
        var e = await _svc.GetByBossEmployeeId(BossEmployeeId, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<List<PunchJustifications>>(e));
    }

    /// <summary>
    /// Aplica justificaciones aprobadas para anular atrasos o ausencias.
    /// </summary>
    /// <param name="request">Rango de fechas y empleado opcional</param>
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyJustifications([FromBody] AttendanceCalculationRequestDto request, CancellationToken ct)
    {
        await _svc.ApplyJustificationsAsync(request.FromDate, request.ToDate, request.EmployeeId, ct);
        return Ok(new { success = true, message = "Justifications applied successfully." });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PunchJustificationsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<PunchJustifications>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PunchJustificationsDto>(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PunchJustificationsUpdateDto dto, CancellationToken ct)
    {
        //Console.WriteLine($"updating justification: {id}");

        try
        {
            // 1. Buscar la entidad existente
            var existingEntity = await _svc.GetByIdAsync(id, ct);
            if (existingEntity == null)
            {
                Console.WriteLine($"Justification not found: {id}");
                return NotFound();
            }

            //Console.WriteLine($"Found entity, mapping updates...");

            // 2. Mapear el DTO a la entidad existente (no crear nueva)
            _mapper.Map(dto, existingEntity);

            //Console.WriteLine($"Mapped updates to existing entity");

            // 3. Actualizar usando la entidad existente
            await _svc.UpdateAsync(id, existingEntity, ct);

            //Console.WriteLine($"Update completed successfully");
            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            //Console.WriteLine($"Concurrency exception: {ex.Message}");
            // Verificar si el registro aún existe
            var stillExists = await _svc.GetByIdAsync(id, ct);
            return stillExists != null ?
                Conflict("Los datos fueron modificados por otro usuario. Por favor, refresque e intente nuevamente.") :
                NotFound("El registro fue eliminado por otro usuario.");
        }
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}


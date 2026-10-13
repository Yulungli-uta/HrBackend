using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.SalaryHistory;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("salary-history")]
public class SalaryHistoryController : ControllerBase
{
    private readonly ISalaryHistoryService _svc;
    private readonly IMapper _mapper;
    public SalaryHistoryController(ISalaryHistoryService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de SalaryHistory.</summary>
    [HttpGet]
    [RequirePermission("SALARY_HISTORY.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<SalaryHistoryDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("SALARY_HISTORY.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<SalaryHistoryDto>(e));
    }

    /// <summary>
    /// Resuelve el RMU de referencia para prellenar "RMU Origen"/"RMU Destino" en el formulario
    /// de Acciones de Personal. Ver <see cref="ISalaryHistoryService.GetReferenceRmuAsync"/> para
    /// el orden de prioridad de fuentes. Al menos uno de los dos parámetros es requerido.
    /// </summary>
    [HttpGet("reference-rmu")]
    [RequirePermission("SALARY_HISTORY.READ")]
    public async Task<IActionResult> GetReferenceRmu(
        [FromQuery] int? employeeId,
        [FromQuery] int? jobId,
        CancellationToken ct)
    {
        if (!employeeId.HasValue && !jobId.HasValue)
            return BadRequest(new { message = "Debe especificar employeeId o jobId." });

        var rmu = await _svc.GetReferenceRmuAsync(employeeId, jobId, ct);
        return Ok(new { rmu });
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("SALARY_HISTORY.CREATE")]
    public async Task<IActionResult> Create([FromBody] SalaryHistoryCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<SalaryHistory>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<SalaryHistoryDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("SALARY_HISTORY.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] SalaryHistoryUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<SalaryHistory>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("SALARY_HISTORY.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}

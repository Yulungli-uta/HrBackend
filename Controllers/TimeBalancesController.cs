using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
using WsUtaSystem.Application.DTOs.TimeBalances.TimeBalancesDTO;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("/timebalances")]
public class TimeBalancesController : ControllerBase
{
    private readonly ITimeBalancesService _svc;
    private readonly IMapper _mapper;

    public TimeBalancesController(ITimeBalancesService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    /// <summary>Lista todos los registros de TimeBalances.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var entities = await _svc.GetAllAsync(ct);
        var dtos = _mapper.Map<List<TimeBalancesResponseDTO>>(entities);
        return Ok(dtos);
    }

    /// <summary>Obtiene un registro por EmployeeID.</summary>
    /// <param name="employeeId">Identificador del empleado</param>
    [HttpGet("{employeeId:int}")]
    public async Task<IActionResult> GetById([FromRoute] int employeeId, CancellationToken ct)
    {
        var entity = await _svc.GetByIdAsync(employeeId, ct);
        return entity is null ?
            NotFound() :
            Ok(_mapper.Map<TimeBalancesResponseDTO>(entity));
    }

    /// <summary>Crea un nuevo registro de TimeBalances.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TimeBalancesCreateDTO dto, CancellationToken ct)
    {
        // Validar modelo
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = _mapper.Map<TimeBalances>(dto);
        var created = await _svc.CreateAsync(entity, ct);

        // En este caso, el ID es EmployeeID
        return CreatedAtAction(
            nameof(GetById),
            new { employeeId = created?.EmployeeID },
            _mapper.Map<TimeBalancesResponseDTO>(created)
        );
    }

    /// <summary>Actualiza un registro existente de TimeBalances.</summary>
    [HttpPut("{employeeId:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int employeeId,
        [FromBody] TimeBalancesUpdateDTO dto,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var entity = _mapper.Map<TimeBalances>(dto);
        entity.EmployeeID = employeeId; // Asegurar que el ID sea el correcto

        await _svc.UpdateAsync(employeeId, entity, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por EmployeeID.</summary>
    [HttpDelete("{employeeId:int}")]
    public async Task<IActionResult> Delete([FromRoute] int employeeId, CancellationToken ct)
    {
        await _svc.DeleteAsync(employeeId, ct);
        return NoContent();
    }

    /// <summary>Obtiene balances por múltiples empleados.</summary>
    [HttpGet("by-employees")]
    public async Task<IActionResult> GetByEmployeeIds(
        [FromQuery, Required] int[] employeeIds,
        CancellationToken ct)
    {
        var entities = await _svc.GetAllAsync(ct);
        var filtered = entities.Where(e => employeeIds.Contains(e.EmployeeID)).ToList();
        return Ok(_mapper.Map<List<TimeBalancesResponseDTO>>(filtered));
    }
}
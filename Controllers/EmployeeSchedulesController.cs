using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.EmployeeSchedules;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("employee-schedules")]
public class EmployeeSchedulesController : ControllerBase
{
    private readonly IEmployeeSchedulesService _svc;
    private readonly IMapper _mapper;
    public EmployeeSchedulesController(IEmployeeSchedulesService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de EmployeeSchedules.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<EmployeeSchedulesDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<EmployeeSchedulesDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    //[HttpPost]
    //public async Task<IActionResult> Create([FromBody] EmployeeSchedulesCreateDto dto, CancellationToken ct)
    //{
    //    var entityObj = _mapper.Map<EmployeeSchedules>(dto);
    //    var created = await _svc.CreateAsync(entityObj, ct);
    //    var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
    //    return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<EmployeeSchedulesDto>(created));
    //}

    ///// <summary>Actualiza un registro existente.</summary>
    //[HttpPut("{id:int}")]
    //public async Task<IActionResult> Update([FromRoute] int id, [FromBody] EmployeeSchedulesUpdateDto dto, CancellationToken ct)
    //{
    //    var entityObj = _mapper.Map<EmployeeSchedules>(dto);
    //    await _svc.UpdateAsync(id, entityObj, ct);
    //    return NoContent();
    //}
    /// <summary>Crea un nuevo registro con control de temporalidad.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeSchedulesCreateDto dto, CancellationToken ct)
    {
        Console.WriteLine($"****************************Creating Employee Schedule with temporal control  {dto.EmployeeId}");
        //var entityObj = _mapper.Map<EmployeeSchedules>(dto);

        //// Usar el método que maneja la temporalidad
        //var result = await _svc.UpdateEmployeeScheduler(entityObj, ct);
        //var created = result.LastOrDefault(); // El último registro es el recién creado

        //return created != null
        //    ? CreatedAtAction(nameof(GetById), new { id = created.EmpScheduleId }, _mapper.Map<EmployeeSchedulesDto>(created))
        //    : BadRequest("No se pudo crear el registro");
        try
        {
            // Crear timeout de 2 minutos
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            // Mapear DTO a entidad
            var employeeSchedule = _mapper.Map<EmployeeSchedules>(dto);

            // Actualizar horario
            var result = await _svc.UpdateEmployeeScheduler(employeeSchedule, linkedCts.Token);

            return Ok(new
            {
                success = true,
                message = "Horario actualizado correctamente",
                data = result
            });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(408, new
            {
                success = false,
                message = "La operación excedió el tiempo de espera"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message
            });
        }

    }

    /// <summary>Actualiza un registro existente con control de temporalidad.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] EmployeeSchedulesUpdateDto dto, CancellationToken ct)
    {
        Console.WriteLine($"****************************Updating Employee Schedule with temporal control {dto}");
        var entityObj = _mapper.Map<EmployeeSchedules>(dto);
        await _svc.UpdateEmployeeScheduler(entityObj, ct);

        return NoContent();
    }
    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}

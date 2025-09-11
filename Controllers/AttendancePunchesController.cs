using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.AttendancePunches;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("attendance/punches")]
public class AttendancePunchesController : ControllerBase
{
    private readonly IAttendancePunchesService _svc;
    private readonly IMapper _mapper;

    public AttendancePunchesController(IAttendancePunchesService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    /// <summary>Lista todos los registros de AttendancePunches.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<AttendancePunchesDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<AttendancePunchesDto>(e));
    }

    /// <summary>Obtiene la última marcación de un empleado.</summary>
    /// <param name="employeeId">ID del empleado</param>
    [HttpGet("last-punch/{employeeId:int}")]
    //[Authorize]
    public async Task<IActionResult> GetLastPunch([FromRoute] int employeeId, CancellationToken ct)
    {
        try
        {
            var lastPunches = await _svc.GetLastPunchAsync(employeeId, ct);

            if (lastPunches == null || !lastPunches.Any())
                return NotFound("No se encontraron marcaciones para este usuario");

            Console.WriteLine($"Last punches data: {_mapper.Map<AttendancePunchesDto>(lastPunches.First())}");
            // Tomamos el primero de la lista (el último por orden de tiempo)
            return Ok(_mapper.Map<AttendancePunchesDto>(lastPunches.First()));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>Obtiene las marcaciones del día actual para un empleado.</summary>
    /// <param name="employeeId">ID del empleado</param>
    [HttpGet("today/{employeeId:int}")]
    //[Authorize]
    public async Task<IActionResult> GetTodayPunches([FromRoute] int employeeId, CancellationToken ct)
    {
        try
        {
            var todayPunches = await _svc.GetTodayPunchesByEmployeeAsync(employeeId, ct);

            if (todayPunches == null || !todayPunches.Any())
                return NotFound("No se encontraron marcaciones para hoy");

            return Ok(_mapper.Map<List<AttendancePunchesDto>>(todayPunches));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>Obtiene las marcaciones de un empleado en un rango de fechas.</summary>
    /// <param name="employeeId">ID del empleado</param>
    /// <param name="startDate">Fecha de inicio</param>
    /// <param name="endDate">Fecha de fin</param>
    [HttpGet("employee/{employeeId:int}/range")]
    //[Authorize]
    public async Task<IActionResult> GetPunchesByEmployeeAndDateRange(
        [FromRoute] int employeeId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        try
        {
            var punches = await _svc.GetPunchesByEmployeeAsync(employeeId, startDate, endDate, ct);

            if (punches == null || !punches.Any())
                return NotFound("No se encontraron marcaciones en el rango de fechas especificado");

            return Ok(_mapper.Map<List<AttendancePunchesDto>>(punches));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>Obtiene las marcaciones en un rango de fechas.</summary>
    /// <param name="startDate">Fecha de inicio</param>
    /// <param name="endDate">Fecha de fin</param>
    [HttpGet("range")]
    //[Authorize]
    public async Task<IActionResult> GetPunchesByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken ct)
    {
        try
        {
            var punches = await _svc.GetPunchesByDateRangeAsync(startDate, endDate, ct);

            if (punches == null || !punches.Any())
                return NotFound("No se encontraron marcaciones en el rango de fechas especificado");

            return Ok(_mapper.Map<List<AttendancePunchesDto>>(punches));
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error interno del servidor: {ex.Message}");
        }
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AttendancePunchesCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<AttendancePunches>(dto);
   
        //// Reemplaza esta línea:
        //// var punchTime = DateTime.Parse(entityObj.PunchTime);   
        //if (!entityObj.PunchTime.HasValue)
        //    return BadRequest("PunchTime es requerido y no puede ser nulo.");

        //// Reemplaza esta línea:
        //// var punchTime = DateTime.Parse(entityObj.PunchTime.Value);

        //// Solución: No necesitas convertir un DateTime a DateTime usando Parse.
        //// Simplemente usa el valor directamente.
        //var punchTime = entityObj.PunchTime.Value;

        //// Especificar explícitamente que es una fecha sin información de zona horaria
        //punchTime = DateTime.SpecifyKind(punchTime, DateTimeKind.Unspecified);

        //// Obtener la zona horaria de Ecuador
        //TimeZoneInfo ecuadorTimeZone;
        //try
        //{
        //    // Para sistemas Linux/macOS
        //    ecuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
        //}
        //catch (TimeZoneNotFoundException)
        //{
        //    // Para sistemas Windows
        //    ecuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        //}

        //var utcTime = TimeZoneInfo.ConvertTimeToUtc(punchTime, ecuadorTimeZone);
        //entityObj.PunchTime = utcTime;
        //Console.WriteLine($"Creating AttendancePunches: {entityObj.EmployeeId}, {entityObj.PunchTime}, {entityObj.PunchType}");
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        //Console.WriteLine("valor id:" + idVal );
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<AttendancePunchesDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] AttendancePunchesUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<AttendancePunches>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
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
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.EmployeeSchedules;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("employee-schedules")]
public class EmployeeSchedulesController : ControllerBase
{
    private readonly IEmployeeSchedulesService _svc;
    private readonly IMapper _mapper;

    public EmployeeSchedulesController(IEmployeeSchedulesService svc, IMapper mapper)
    {
        _svc = svc;
        _mapper = mapper;
    }

    /// <summary>Lista todos los registros de EmployeeSchedules.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<EmployeeSchedulesDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Retorna un resultado paginado de horarios de empleados.</summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = "asc",
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        var pagedEntities = await _svc.GetPagedAsync(page, pageSize, ct);
        var pagedDto = new PagedResult<EmployeeSchedulesDto>
        {
            Items = _mapper.Map<List<EmployeeSchedulesDto>>(pagedEntities.Items),
            Page = pagedEntities.Page,
            PageSize = pagedEntities.PageSize,
            TotalCount = pagedEntities.TotalCount
        };
        return Ok(pagedDto);
    }

    /// <summary>Obtiene un registro por ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<EmployeeSchedulesDto>(e));
    }

    /// <summary>Crea un nuevo registro con control de temporalidad.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeSchedulesCreateDto dto, CancellationToken ct)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
            var employeeSchedule = _mapper.Map<EmployeeSchedules>(dto);
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
                message = "La operacion excedio el tiempo de espera"
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

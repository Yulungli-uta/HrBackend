using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.VwEmployeeScheduleAtDate;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/vw-employee-schedule-at-date")]
public class VwEmployeeScheduleAtDateController : ControllerBase
{
    private readonly IVwEmployeeScheduleAtDateService _svc;
    private readonly IMapper _mapper;
    public VwEmployeeScheduleAtDateController(IVwEmployeeScheduleAtDateService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los horarios de empleados por fecha.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<VwEmployeeScheduleAtDateDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene horarios por ID de empleado.</summary>
    /// <param name="employeeId">ID del empleado</param>
    [HttpGet("by-employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployeeId([FromRoute] int employeeId, CancellationToken ct) =>
        Ok(_mapper.Map<List<VwEmployeeScheduleAtDateDto>>(await _svc.GetByEmployeeIdAsync(employeeId, ct)));

    /// <summary>Obtiene horarios por fecha específica.</summary>
    /// <param name="date">Fecha a consultar</param>
    [HttpGet("by-date")]
    public async Task<IActionResult> GetByDate([FromQuery] DateTime date, CancellationToken ct) =>
        Ok(_mapper.Map<List<VwEmployeeScheduleAtDateDto>>(await _svc.GetByDateAsync(date, ct)));
}


using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.VwPunchDay;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/vw-punch-day")]
public class VwPunchDayController : ControllerBase
{
    private readonly IVwPunchDayService _svc;
    private readonly IMapper _mapper;
    public VwPunchDayController(IVwPunchDayService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todas las picadas diarias.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<VwPunchDayDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene picadas por ID de empleado.</summary>
    /// <param name="employeeId">ID del empleado</param>
    [HttpGet("by-employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployeeId([FromRoute] int employeeId, CancellationToken ct) =>
        Ok(_mapper.Map<List<VwPunchDayDto>>(await _svc.GetByEmployeeIdAsync(employeeId, ct)));

    /// <summary>Obtiene picadas por rango de fechas.</summary>
    /// <param name="fromDate">Fecha desde</param>
    /// <param name="toDate">Fecha hasta</param>
    [HttpGet("by-date-range")]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, CancellationToken ct) =>
        Ok(_mapper.Map<List<VwPunchDayDto>>(await _svc.GetByDateRangeAsync(fromDate, toDate, ct)));
}


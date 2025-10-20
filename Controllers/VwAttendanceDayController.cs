using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.VwAttendanceDay;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/vw-attendance-day")]
public class VwAttendanceDayController : ControllerBase
{
    private readonly IVwAttendanceDayService _svc;
    private readonly IMapper _mapper;
    public VwAttendanceDayController(IVwAttendanceDayService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los días de asistencia esperados vs trabajados.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<VwAttendanceDayDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene asistencia por ID de empleado.</summary>
    /// <param name="employeeId">ID del empleado</param>
    [HttpGet("by-employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployeeId([FromRoute] int employeeId, CancellationToken ct) =>
        Ok(_mapper.Map<List<VwAttendanceDayDto>>(await _svc.GetByEmployeeIdAsync(employeeId, ct)));

    /// <summary>Obtiene asistencia por rango de fechas.</summary>
    /// <param name="fromDate">Fecha desde</param>
    /// <param name="toDate">Fecha hasta</param>
    [HttpGet("by-date-range")]
    public async Task<IActionResult> GetByDateRange([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate, CancellationToken ct) =>
        Ok(_mapper.Map<List<VwAttendanceDayDto>>(await _svc.GetByDateRangeAsync(fromDate, toDate, ct)));
}


using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.VwLeaveWindows;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/vw-leave-windows")]
public class VwLeaveWindowsController : ControllerBase
{
    private readonly IVwLeaveWindowsService _svc;
    private readonly IMapper _mapper;
    public VwLeaveWindowsController(IVwLeaveWindowsService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todas las ventanas de ausencias justificadas.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<VwLeaveWindowsDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene ausencias por ID de empleado.</summary>
    /// <param name="employeeId">ID del empleado</param>
    [HttpGet("by-employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployeeId([FromRoute] int employeeId, CancellationToken ct) =>
        Ok(_mapper.Map<List<VwLeaveWindowsDto>>(await _svc.GetByEmployeeIdAsync(employeeId, ct)));

    /// <summary>Obtiene ausencias por tipo.</summary>
    /// <param name="leaveType">Tipo de ausencia (VACATION, PERMISSION)</param>
    [HttpGet("by-type/{leaveType}")]
    public async Task<IActionResult> GetByLeaveType([FromRoute] string leaveType, CancellationToken ct) =>
        Ok(_mapper.Map<List<VwLeaveWindowsDto>>(await _svc.GetByLeaveTypeAsync(leaveType, ct)));
}


using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.DTOs.TimeRecoveryLogs;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("time-recovery/logs")]
public class TimeRecoveryLogsController : ControllerBase
{
    private readonly ITimeRecoveryLogsService _svc;
    private readonly IMapper _mapper;
    public TimeRecoveryLogsController(ITimeRecoveryLogsService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de TimeRecoveryLogs.</summary>
    [HttpGet]
    [RequirePermission("TIME_RECOVERY.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<TimeRecoveryLogsDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("TIME_RECOVERY.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<TimeRecoveryLogsDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    [RequirePermission("TIME_RECOVERY.CREATE")]
    public async Task<IActionResult> Create([FromBody] TimeRecoveryLogsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<TimeRecoveryLogs>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<TimeRecoveryLogsDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("TIME_RECOVERY.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TimeRecoveryLogsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<TimeRecoveryLogs>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("TIME_RECOVERY.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}

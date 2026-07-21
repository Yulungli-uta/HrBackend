using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.Audit;
using WsUtaSystem.Models;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _svc;
    private readonly IMapper _mapper;
    public AuditController(IAuditService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de Audit.</summary>
    [HttpGet]
    [RequirePermission("AUDIT.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<AuditDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("AUDIT.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<AuditDto>(e));
    }

    /// <summary>
    /// Crea un nuevo registro. En la práctica, los registros de auditoría los genera
    /// <c>AuditSaveChangesInterceptor</c> automáticamente — este endpoint queda restringido
    /// a ADMIN.ACCESS (sin rol de negocio con grant directo) para no ser una vía de inserción
    /// manual de auditoría por usuarios comunes.
    /// </summary>
    [HttpPost]
    [RequirePermission("AUDIT.CREATE")]
    public async Task<IActionResult> Create([FromBody] AuditCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Audit>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<AuditDto>(created));
    }

    // Sin PUT/DELETE: el log de auditoría es append-only por diseño (no se edita ni se borra).
}

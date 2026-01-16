using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.FinancialCertification;
using WsUtaSystem.Models;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Controllers;


[ApiController]
[Route("financial-certification")]
public class FinancialCertificationController : ControllerBase
{
    private readonly IFinancialCertificationService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _user;
    private readonly ILogger<FinancialCertificationController> _logger;
    public FinancialCertificationController(IFinancialCertificationService svc, IMapper mapper, ICurrentUserService userService, ILogger<FinancialCertificationController> logger) { 
        _svc = svc; 
        _mapper = mapper;
        _logger = logger;
        _user = userService;
    }

    /// <summary>Lista todos los registros de FinancialCertification.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<FinancialCertificationDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<FinancialCertificationDto>(e));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FinancialCertificationCreateDto dto, CancellationToken ct)
    {
      var claimsDump = User?.Claims
      .Select(c => $"{c.Type}={c.Value}")
      .ToArray();

            _logger.LogInformation("Claims: {Claims}", claimsDump);
            _logger.LogInformation("IsAuth={IsAuth} NameId={NameId} Sub={Sub} Email={Email} EmpClaim={EmpClaim} userid {userId}",
              _user.IsAuthenticated,
              User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
              User?.FindFirst("sub")?.Value,
              _user.Email,
              User?.FindFirst("employeeId")?.Value,
              User?.FindFirst("userId")?.Value
            );
        var entityObj = _mapper.Map<FinancialCertification>(dto);
        //entityObj.CreatedAt = DateTime.Now;
        //entityObj.CreatedBy = _user.IsAuthenticated ? _user.EmployeeId : null;

        _logger.LogInformation(
            "****************Creating FinancialCertification: {CreatedAt} createdby {EmployeeId}",
            DateTime.Now,
            _user.EmployeeId
        );
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<FinancialCertificationDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] FinancialCertificationUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<FinancialCertification>(dto);
        //entityObj.UpdatedAt = DateTime.Now;
        //entityObj.UpdatedBy = _user.IsAuthenticated ? _user.EmployeeId : null;
        _logger.LogInformation("****************updateing FinancialCertification: {createat} createdby {EmployeeId}", DateTime.Now, _user.EmployeeId);
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


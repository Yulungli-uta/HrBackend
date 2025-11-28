using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.BankAccounts;
using WsUtaSystem.Models;
using WsUtaSystem.Infrastructure.Controller;

namespace WsUtaSystem.Controllers;

[ApiController]
[Route("cv/bank-accounts")]
public class BankAccountsController : ControllerBase
{
    private readonly IBankAccountsService _svc;
    private readonly IMapper _mapper;
    public BankAccountsController(IBankAccountsService svc, IMapper mapper) { _svc = svc; _mapper = mapper; }

    /// <summary>Lista todos los registros de BankAccounts.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<BankAccountsDto>>(await _svc.GetAllAsync(ct)));

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        return e is null ? NotFound() : Ok(_mapper.Map<BankAccountsDto>(e));
    }

    /// <summary>Obtiene todos los cuentas bancarias de una persona.</summary>
    /// <param name="personId">ID de la persona</param>
    [HttpGet("person/{personId:int}")]
    public async Task<IActionResult> GetByPersonId([FromRoute] int personId, CancellationToken ct)
    {
        var accounts = await _svc.GetByPersonIdAsync(personId);
        return Ok(_mapper.Map<List<BankAccountsDto>(accounts));
    }

    /// <summary>Crea un nuevo registro.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BankAccountsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<BankAccounts>(dto);
        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<BankAccountsDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] BankAccountsUpdateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<BankAccounts>(dto);
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

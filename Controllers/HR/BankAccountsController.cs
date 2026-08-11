using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.BankAccounts;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("cv/bank-accounts")]
public class BankAccountsController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IBankAccountsService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    public BankAccountsController(IBankAccountsService svc, IMapper mapper, ICurrentUserService currentUser)
    {
        _svc = svc;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    /// <summary>Lista todos los registros de BankAccounts. Requiere rol de RRHH/administración.</summary>
    [HttpGet]
    [RequirePermission("BANK_ACCOUNTS.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver todas las cuentas bancarias del sistema.");

        return Ok(_mapper.Map<List<BankAccountsDto>>(await _svc.GetAllAsync(ct)));
    }

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("BANK_ACCOUNTS.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        if (e is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != e.PersonId)
            return Forbid403("No puede consultar cuentas bancarias de otra persona.");

        return Ok(_mapper.Map<BankAccountsDto>(e));
    }

    /// <summary>Obtiene todas las cuentas bancarias de una persona.</summary>
    /// <param name="personId">ID de la persona</param>
    [HttpGet("person/{personId:int}")]
    [RequirePermission("BANK_ACCOUNTS.READ")]
    public async Task<IActionResult> GetByPersonId([FromRoute] int personId, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != personId)
            return Forbid403("No puede consultar cuentas bancarias de otra persona.");

        var accounts = await _svc.GetByPersonIdAsync(personId);
        return Ok(_mapper.Map<List<BankAccountsDto>>(accounts));
    }

    /// <summary>Crea un nuevo registro. El PersonId del payload se ignora salvo rol elevado —
    /// nunca se confía en el cliente para "de quién" es el registro.</summary>
    [HttpPost]
    [RequirePermission("BANK_ACCOUNTS.CREATE")]
    public async Task<IActionResult> Create([FromBody] BankAccountsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<BankAccounts>(dto);
        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entityObj.PersonId = myPersonId.Value;
        }

        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<BankAccountsDto>(created));
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("BANK_ACCOUNTS.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] BankAccountsUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != current.PersonId)
            return Forbid403("No puede editar cuentas bancarias de otra persona.");

        var entityObj = _mapper.Map<BankAccounts>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("BANK_ACCOUNTS.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != current.PersonId)
            return Forbid403("No puede eliminar cuentas bancarias de otra persona.");

        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}

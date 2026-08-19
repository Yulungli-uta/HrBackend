using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.FamilyBurden;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("cv/family-burden")]
public class FamilyBurdenController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IFamilyBurdenService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    public FamilyBurdenController(IFamilyBurdenService svc, IMapper mapper, ICurrentUserService currentUser)
    {
        _svc = svc;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    /// <summary>Lista todos los registros de FamilyBurden. Requiere rol de RRHH/administración.</summary>
    [HttpGet]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver todas las cargas familiares del sistema.");

        return Ok(_mapper.Map<List<FamilyBurdenDto>>(await _svc.GetAllAsync(ct)));
    }

    /// <summary>Obtiene un registro por ID.</summary>
    /// <param name="id">Identificador</param>
    [HttpGet("{id:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var e = await _svc.GetByIdAsync(id, ct);
        if (e is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != e.PersonId)
            return Forbid403("No puede consultar cargas familiares de otra persona.");

        return Ok(_mapper.Map<FamilyBurdenDto>(e));
    }

    /// <summary>Contadores agregados (total, por estado, con discapacidad) para dato gerencial.</summary>
    [HttpGet("stats")]
    [RequirePermission("FAMILY_BURDEN.APPROVE")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
        => Ok(await _svc.GetStatsAsync(ct));

    /// <summary>Listado paginado para la pantalla de validación, filtrable por estado y por cédula/nombre del empleado titular.</summary>
    [HttpGet("validation")]
    [RequirePermission("FAMILY_BURDEN.APPROVE")]
    public async Task<IActionResult> GetForValidation(
        [FromQuery] int? statusTypeId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _svc.GetForValidationAsync(statusTypeId, search, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Aprueba una carga familiar registrada.</summary>
    [HttpPost("{id:int}/approve")]
    [RequirePermission("FAMILY_BURDEN.APPROVE")]
    public async Task<IActionResult> Approve([FromRoute] int id, CancellationToken ct)
    {
        var approverId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede aprobar cargas familiares.");

        await _svc.ApproveAsync(id, approverId, ct);
        return NoContent();
    }

    /// <summary>Rechaza una carga familiar registrada con motivo obligatorio.</summary>
    [HttpPost("{id:int}/reject")]
    [RequirePermission("FAMILY_BURDEN.APPROVE")]
    public async Task<IActionResult> Reject([FromRoute] int id, [FromBody] FamilyBurdenRejectDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            return BadRequest(new { message = "El motivo de rechazo es obligatorio." });

        var rejecterId = _currentUser.EmployeeId
            ?? throw new InvalidOperationException("Usuario sin EmployeeId no puede rechazar cargas familiares.");

        await _svc.RejectAsync(id, rejecterId, dto.Reason, ct);
        return NoContent();
    }

    /// <summary>Obtiene toda la carga familiar de una persona.</summary>
    /// <param name="personId">ID de la persona</param>
    [HttpGet("person/{personId:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetByPersonId([FromRoute] int personId, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != personId)
            return Forbid403("No puede consultar cargas familiares de otra persona.");

        var familyMembers = await _svc.GetByPersonIdAsync(personId);
        return Ok(_mapper.Map<List<FamilyBurdenDto>>(familyMembers));
    }

    /// <summary>Crea un nuevo registro. El PersonId del payload se ignora salvo rol elevado —
    /// nunca se confía en el cliente para "de quién" es el registro.</summary>
    [HttpPost]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    public async Task<IActionResult> Create([FromBody] FamilyBurdenCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<FamilyBurden>(dto);
        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entityObj.PersonId = myPersonId.Value;
        }

        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<FamilyBurdenDto>(created));
    }

    /// <summary>Crea una carga familiar junto con su documento de respaldo en una sola llamada transaccional.</summary>
    [HttpPost("with-document")]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateWithDocument([FromForm] FamilyBurdenWithDocumentCreateDto dto, CancellationToken ct)
    {
        var entity = new FamilyBurden
        {
            PersonId = dto.PersonId,
            DependentId = dto.DependentId,
            IdentificationTypeId = dto.IdentificationTypeId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            BirthDate = dto.BirthDate,
            DisabilityTypeId = dto.DisabilityTypeId,
            DisabilityPercentage = dto.DisabilityPercentage,
        };

        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entity.PersonId = myPersonId.Value;
        }

        var (created, storedFile, error) = await _svc.CreateWithDocumentAsync(
            entity, dto.File, dto.DocumentTypeId, dto.DisabilityFile, dto.DisabilityDocumentTypeId, ct);
        if (error != null) return BadRequest(new { message = error });

        var result = new FamilyBurdenWithDocumentResultDto
        {
            FamilyBurden = _mapper.Map<FamilyBurdenDto>(created),
            StoredFile = storedFile != null ? _mapper.Map<Application.DTOs.StoredFile.StoredFileDto>(storedFile) : null,
        };
        return CreatedAtAction(nameof(GetById), new { id = created.BurdenId }, result);
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] FamilyBurdenUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != current.PersonId)
            return Forbid403("No puede editar cargas familiares de otra persona.");

        var entityObj = _mapper.Map<FamilyBurden>(dto);
        await _svc.UpdateAsync(id, entityObj, ct);
        return NoContent();
    }

    /// <summary>Elimina un registro por ID.</summary>
    [HttpDelete("{id:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.DELETE")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != current.PersonId)
            return Forbid403("No puede eliminar cargas familiares de otra persona.");

        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}

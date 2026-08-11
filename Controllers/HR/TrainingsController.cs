using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Trainings;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("cv/trainings")]
public class TrainingsController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly ITrainingsService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    public TrainingsController(ITrainingsService svc, IMapper mapper, ICurrentUserService currentUser)
    {
        _svc = svc;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    /// <summary>Lista todos los registros de Trainings. Requiere rol de RRHH/administración.</summary>
    [HttpGet]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver todas las capacitaciones del sistema.");

        return Ok(_mapper.Map<List<TrainingsDto>>(await _svc.GetAllAsync(ct)));
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
            return Forbid403("No puede consultar capacitaciones de otra persona.");

        return Ok(_mapper.Map<TrainingsDto>(e));
    }

    /// <summary>Obtiene todas las capacitaciones de una persona.</summary>
    /// <param name="personId">ID de la persona</param>
    [HttpGet("person/{personId:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetByPersonId([FromRoute] int personId, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != personId)
            return Forbid403("No puede consultar capacitaciones de otra persona.");

        var trainings = await _svc.GetByPersonIdAsync(personId);
        return Ok(_mapper.Map<List<TrainingsDto>>(trainings));
    }

    /// <summary>Crea un nuevo registro. El PersonId del payload se ignora salvo rol elevado —
    /// nunca se confía en el cliente para "de quién" es el registro.</summary>
    [HttpPost]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    public async Task<IActionResult> Create([FromBody] TrainingsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Trainings>(dto);
        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entityObj.PersonId = myPersonId.Value;
        }

        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<TrainingsDto>(created));
    }

    /// <summary>Crea una capacitación junto con su certificado de respaldo en una sola llamada transaccional.</summary>
    [HttpPost("with-document")]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateWithDocument([FromForm] TrainingWithDocumentCreateDto dto, CancellationToken ct)
    {
        var entity = new Trainings
        {
            PersonId = dto.PersonId,
            Location = dto.Location,
            Title = dto.Title,
            Institution = dto.Institution,
            KnowledgeAreaTypeId = dto.KnowledgeAreaTypeId,
            EventTypeId = dto.EventTypeId,
            CertifiedBy = dto.CertifiedBy,
            CertificateTypeId = dto.CertificateTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Hours = dto.Hours,
            ApprovalTypeId = dto.ApprovalTypeId,
        };

        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entity.PersonId = myPersonId.Value;
        }

        var (created, storedFile, error) = await _svc.CreateWithDocumentAsync(entity, dto.File, dto.DocumentTypeId, ct);
        if (error != null) return BadRequest(new { message = error });

        var result = new TrainingWithDocumentResultDto
        {
            Training = _mapper.Map<TrainingsDto>(created),
            StoredFile = storedFile != null ? _mapper.Map<Application.DTOs.StoredFile.StoredFileDto>(storedFile) : null,
        };
        return CreatedAtAction(nameof(GetById), new { id = created.TrainingId }, result);
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TrainingsUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != current.PersonId)
            return Forbid403("No puede editar capacitaciones de otra persona.");

        var entityObj = _mapper.Map<Trainings>(dto);
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
            return Forbid403("No puede eliminar capacitaciones de otra persona.");

        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}

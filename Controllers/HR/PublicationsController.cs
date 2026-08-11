using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Publications;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("cv/publications")]
public class PublicationsController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IPublicationsService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    public PublicationsController(IPublicationsService svc, IMapper mapper, ICurrentUserService currentUser)
    {
        _svc = svc;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    /// <summary>Lista todos los registros de Publications. Requiere rol de RRHH/administración.</summary>
    [HttpGet]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver todas las publicaciones del sistema.");

        return Ok(_mapper.Map<List<PublicationsDto>>(await _svc.GetAllAsync(ct)));
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
            return Forbid403("No puede consultar publicaciones de otra persona.");

        return Ok(_mapper.Map<PublicationsDto>(e));
    }

    /// <summary>Obtiene todas las publicaciones de una persona.</summary>
    /// <param name="personId">ID de la persona</param>
    [HttpGet("person/{personId:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetByPersonId([FromRoute] int personId, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != personId)
            return Forbid403("No puede consultar publicaciones de otra persona.");

        var publications = await _svc.GetByPersonIdAsync(personId);
        return Ok(_mapper.Map<List<PublicationsDto>>(publications));
    }

    /// <summary>Crea un nuevo registro. El PersonId del payload se ignora salvo rol elevado —
    /// nunca se confía en el cliente para "de quién" es el registro.</summary>
    [HttpPost]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    public async Task<IActionResult> Create([FromBody] PublicationsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Publications>(dto);
        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entityObj.PersonId = myPersonId.Value;
        }

        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<PublicationsDto>(created));
    }

    /// <summary>Crea una publicación junto con su documento de respaldo en una sola llamada transaccional.</summary>
    [HttpPost("with-document")]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateWithDocument([FromForm] PublicationWithDocumentCreateDto dto, CancellationToken ct)
    {
        var entity = new Publications
        {
            PersonId = dto.PersonId,
            Location = dto.Location,
            PublicationTypeId = dto.PublicationTypeId,
            IsIndexed = dto.IsIndexed,
            JournalTypeId = dto.JournalTypeId,
            Issn_Isbn = dto.Issn_Isbn,
            JournalName = dto.JournalName,
            JournalNumber = dto.JournalNumber,
            Volume = dto.Volume,
            Pages = dto.Pages,
            KnowledgeAreaTypeId = dto.KnowledgeAreaTypeId,
            SubAreaTypeId = dto.SubAreaTypeId,
            AreaTypeId = dto.AreaTypeId,
            Title = dto.Title,
            OrganizedBy = dto.OrganizedBy,
            EventName = dto.EventName,
            EventEdition = dto.EventEdition,
            PublicationDate = dto.PublicationDate,
            UTAffiliation = dto.UTAffiliation,
        };

        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entity.PersonId = myPersonId.Value;
        }

        var (created, storedFile, error) = await _svc.CreateWithDocumentAsync(entity, dto.File, dto.DocumentTypeId, ct);
        if (error != null) return BadRequest(new { message = error });

        var result = new PublicationWithDocumentResultDto
        {
            Publication = _mapper.Map<PublicationsDto>(created),
            StoredFile = storedFile != null ? _mapper.Map<Application.DTOs.StoredFile.StoredFileDto>(storedFile) : null,
        };
        return CreatedAtAction(nameof(GetById), new { id = created.PublicationId }, result);
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] PublicationsUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != current.PersonId)
            return Forbid403("No puede editar publicaciones de otra persona.");

        var entityObj = _mapper.Map<Publications>(dto);
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
            return Forbid403("No puede eliminar publicaciones de otra persona.");

        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}

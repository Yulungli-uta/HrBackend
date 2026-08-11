using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.DTOs.Books;
using WsUtaSystem.Models;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;


[ApiController]
[Route("cv/books")]
public class BooksController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IBooksService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    public BooksController(IBooksService svc, IMapper mapper, ICurrentUserService currentUser)
    {
        _svc = svc;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    /// <summary>Lista todos los registros de Books. Requiere rol de RRHH/administración.</summary>
    [HttpGet]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver todos los libros del sistema.");

        return Ok(_mapper.Map<List<BooksDto>>(await _svc.GetAllAsync(ct)));
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
            return Forbid403("No puede consultar libros de otra persona.");

        return Ok(_mapper.Map<BooksDto>(e));
    }

    /// <summary>Obtiene todos los libros de una persona.</summary>
    /// <param name="personId">ID de la persona</param>
    [HttpGet("person/{personId:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetByPersonId([FromRoute] int personId, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != personId)
            return Forbid403("No puede consultar libros de otra persona.");

        var books = await _svc.GetByPersonIdAsync(personId);
        return Ok(_mapper.Map<List<BooksDto>>(books));
    }

    /// <summary>Crea un nuevo registro. El PersonId del payload se ignora salvo rol elevado —
    /// nunca se confía en el cliente para "de quién" es el registro.</summary>
    [HttpPost]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    public async Task<IActionResult> Create([FromBody] BooksCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<Books>(dto);
        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entityObj.PersonId = myPersonId.Value;
        }

        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<BooksDto>(created));
    }

    /// <summary>Crea un libro junto con su documento de respaldo en una sola llamada transaccional.</summary>
    [HttpPost("with-document")]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateWithDocument([FromForm] BookWithDocumentCreateDto dto, CancellationToken ct)
    {
        var entity = new Books
        {
            PersonId = dto.PersonId,
            Title = dto.Title,
            PeerReviewed = dto.PeerReviewed,
            ISBN = dto.ISBN,
            Publisher = dto.Publisher,
            CountryId = dto.CountryId,
            City = dto.City,
            KnowledgeAreaTypeId = dto.KnowledgeAreaTypeId,
            SubAreaTypeId = dto.SubAreaTypeId,
            AreaTypeId = dto.AreaTypeId,
            VolumeCount = dto.VolumeCount,
            ParticipationTypeId = dto.ParticipationTypeId,
            PublicationDate = dto.PublicationDate,
            UTAffiliation = dto.UTAffiliation,
            UTASponsorship = dto.UTASponsorship,
            BookTypeId = dto.BookTypeId,
        };

        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entity.PersonId = myPersonId.Value;
        }

        var (created, storedFile, error) = await _svc.CreateWithDocumentAsync(entity, dto.File, dto.DocumentTypeId, ct);
        if (error != null) return BadRequest(new { message = error });

        var result = new BookWithDocumentResultDto
        {
            Book = _mapper.Map<BooksDto>(created),
            StoredFile = storedFile != null ? _mapper.Map<Application.DTOs.StoredFile.StoredFileDto>(storedFile) : null,
        };
        return CreatedAtAction(nameof(GetById), new { id = created.BookId }, result);
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] BooksUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != current.PersonId)
            return Forbid403("No puede editar libros de otra persona.");

        var entityObj = _mapper.Map<Books>(dto);
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
            return Forbid403("No puede eliminar libros de otra persona.");

        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}

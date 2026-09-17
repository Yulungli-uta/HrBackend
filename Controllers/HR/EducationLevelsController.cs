using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.EducationLevels;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Controller;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("cv/education-levels")]
public class EducationLevelsController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IEducationLevelsService _svc;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IEducationLevelSyncService _sync;
    public EducationLevelsController(
        IEducationLevelsService svc, IMapper mapper, ICurrentUserService currentUser, IEducationLevelSyncService sync)
    {
        _svc = svc;
        _mapper = mapper;
        _currentUser = currentUser;
        _sync = sync;
    }

    /// <summary>
    /// Previsualiza la sincronización con SENESCYT (vía DINARDAP) para una persona: cuántos
    /// títulos hay, cuántos ya están registrados y cuántos se crearían. No persiste nada.
    /// </summary>
    [HttpGet("person/{personId:int}/senescyt-sync/preview")]
    [RequirePermission("DINARDAP_HR.READ")]
    public async Task<IActionResult> PreviewSenescytSync([FromRoute] int personId, CancellationToken ct)
    {
        try
        {
            return Ok(await _sync.PreviewAsync(personId, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Confirma la sincronización: crea únicamente los títulos que no existían todavía.</summary>
    [HttpPost("person/{personId:int}/senescyt-sync/confirm")]
    [RequirePermission("DINARDAP_HR.READ")]
    public async Task<IActionResult> ConfirmSenescytSync([FromRoute] int personId, CancellationToken ct)
    {
        try
        {
            return Ok(await _sync.ConfirmAsync(personId, ct));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Lista todos los registros de EducationLevels. Requiere rol de RRHH/administración.</summary>
    [HttpGet]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole))
            return Forbid403("No tiene permisos para ver todos los registros de formación académica del sistema.");

        return Ok(_mapper.Map<List<EducationLevelsDto>>(await _svc.GetAllAsync(ct)));
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
            return Forbid403("No puede consultar registros de formación académica de otra persona.");

        return Ok(_mapper.Map<EducationLevelsDto>(e));
    }

    /// <summary>Obtiene todos los niveles educativos de una persona.</summary>
    /// <param name="personId">ID de la persona</param>
    [HttpGet("person/{personId:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetByPersonId([FromRoute] int personId, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != personId)
            return Forbid403("No puede consultar registros de formación académica de otra persona.");

        var educationLevels = await _svc.GetByPersonIdAsync(personId);
        return Ok(_mapper.Map<List<EducationLevelsDto>>(educationLevels));
    }

    /// <summary>Crea un nuevo registro. El PersonId del payload se ignora salvo rol elevado —
    /// nunca se confía en el cliente para "de quién" es el registro.</summary>
    [HttpPost]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    public async Task<IActionResult> Create([FromBody] EducationLevelsCreateDto dto, CancellationToken ct)
    {
        var entityObj = _mapper.Map<EducationLevels>(dto);
        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entityObj.PersonId = myPersonId.Value;
        }

        var created = await _svc.CreateAsync(entityObj, ct);
        var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
        return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<EducationLevelsDto>(created));
    }

    /// <summary>
    /// Crea un registro junto con su documento de respaldo (título/certificado) en una sola
    /// llamada, con garantía transaccional: si el archivo se sube pero el registro no se
    /// pudo guardar en BD (o viceversa), no queda ninguno de los dos a medias.
    /// </summary>
    [HttpPost("with-document")]
    [RequirePermission("EMPLOYEE_PROFILE.CREATE")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateWithDocument([FromForm] EducationLevelWithDocumentCreateDto dto, CancellationToken ct)
    {
        var entity = new EducationLevels
        {
            PersonId = dto.PersonId,
            EducationLevelTypeId = dto.EducationLevelTypeId,
            InstitutionId = dto.InstitutionId,
            Title = dto.Title,
            Specialty = dto.Specialty,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Grade = dto.Grade,
            Location = dto.Location,
            Score = dto.Score,
            SenescytRegistrationNumber = dto.SenescytRegistrationNumber,
            SiiesGradoTypeId = dto.SiiesGradoTypeId,
            SenescytGraduationDate = dto.SenescytGraduationDate,
            SenescytRegistrationDate = dto.SenescytRegistrationDate,
            SenescytType = dto.SenescytType,
            // Manual siempre en este endpoint - el sincronizador con DINARDAP no pasa por aquí.
            Source = "Manual",
        };

        if (!ElevatedRoles.Any(User.IsInRole))
        {
            var myPersonId = await _currentUser.GetPersonIdAsync(ct);
            if (myPersonId is null) return Forbid403("No se pudo determinar la persona asociada al usuario autenticado.");
            entity.PersonId = myPersonId.Value;
        }

        var (created, storedFile, error) = await _svc.CreateWithDocumentAsync(entity, dto.File, dto.DocumentTypeId, ct);
        if (error != null) return BadRequest(new { message = error });

        var result = new EducationLevelWithDocumentResultDto
        {
            EducationLevel = _mapper.Map<EducationLevelsDto>(created),
            StoredFile = storedFile != null ? _mapper.Map<Application.DTOs.StoredFile.StoredFileDto>(storedFile) : null,
        };
        return CreatedAtAction(nameof(GetById), new { id = created.EducationId }, result);
    }

    /// <summary>Actualiza un registro existente.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("EMPLOYEE_PROFILE.UPDATE")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] EducationLevelsUpdateDto dto, CancellationToken ct)
    {
        var current = await _svc.GetByIdAsync(id, ct);
        if (current is null) return NotFound();

        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != current.PersonId)
            return Forbid403("No puede editar registros de formación académica de otra persona.");

        var entityObj = _mapper.Map<EducationLevels>(dto);
        // El DTO de edición manual no expone Source/*Original a propósito - SetValues (usado
        // por el UpdateAsync genérico) sobreescribe TODO lo ausente del DTO con el default de
        // la clase, así que sin esto un título sincronizado perdería su marca de origen y el
        // texto crudo de DINARDAP en cualquier edición manual. Mismo patrón de bug ya visto
        // antes en este proyecto con SetValues genérico (Jobs/JobActivity).
        entityObj.Source = current.Source;
        entityObj.SenescytNivelNombreOriginal = current.SenescytNivelNombreOriginal;
        entityObj.InstitutionNameOriginal = current.InstitutionNameOriginal;
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
            return Forbid403("No puede eliminar registros de formación académica de otra persona.");

        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}

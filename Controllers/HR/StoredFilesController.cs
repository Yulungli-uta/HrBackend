using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.StoredFile;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR
{
    [ApiController]
    [Route("storefiles")]
    public class StoredFilesController : ControllerBase
    {
        private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

        private readonly IStoredFileService _svc;
        private readonly IMapper _mapper;
        private readonly ILogger<StoredFilesController> _logger;
        private readonly ICurrentUserService _currentUser;
        private readonly IEducationLevelsService _educationLevelsSvc;
        private readonly IPublicationsService _publicationsSvc;
        private readonly IFamilyBurdenService _familyBurdenSvc;
        private readonly IWorkExperiencesService _workExperiencesSvc;
        private readonly ITrainingsService _trainingsSvc;
        private readonly ILanguagesService _languagesSvc;
        private readonly IBooksService _booksSvc;

        public StoredFilesController(
            IStoredFileService svc,
            IMapper mapper,
            ILogger<StoredFilesController> logger,
            ICurrentUserService currentUser,
            IEducationLevelsService educationLevelsSvc,
            IPublicationsService publicationsSvc,
            IFamilyBurdenService familyBurdenSvc,
            IWorkExperiencesService workExperiencesSvc,
            ITrainingsService trainingsSvc,
            ILanguagesService languagesSvc,
            IBooksService booksSvc)
        {
            _svc = svc;
            _mapper = mapper;
            _logger = logger;
            _currentUser = currentUser;
            _educationLevelsSvc = educationLevelsSvc;
            _publicationsSvc = publicationsSvc;
            _familyBurdenSvc = familyBurdenSvc;
            _workExperiencesSvc = workExperiencesSvc;
            _trainingsSvc = trainingsSvc;
            _languagesSvc = languagesSvc;
            _booksSvc = booksSvc;
        }

        /// <summary>
        /// Resuelve el PersonId dueño del registro de hoja de vida al que pertenece este archivo,
        /// SOLO para los tipos de entidad de hoja de vida conocidos (los 7 módulos con adjuntos
        /// construidos en esta sesión). Para cualquier otro entityType (contratos, resoluciones,
        /// etc. — StoredFilesController es genérico para todo el sistema) retorna null a propósito:
        /// ese universo de entidades queda fuera del alcance de este control específico, no se
        /// aplica ninguna restricción de propiedad ahí (comportamiento sin cambios).
        /// </summary>
        private async Task<int?> ResolveHojaDeVidaOwnerPersonIdAsync(string entityType, string entityId, CancellationToken ct)
        {
            if (!int.TryParse(entityId, out var id)) return null;

            return entityType.ToUpperInvariant() switch
            {
                "EDUCATION_LEVEL" => (await _educationLevelsSvc.GetByIdAsync(id, ct))?.PersonId,
                "PUBLICATION" => (await _publicationsSvc.GetByIdAsync(id, ct))?.PersonId,
                "FAMILY_MEMBER" => (await _familyBurdenSvc.GetByIdAsync(id, ct))?.PersonId,
                "WORK_EXPERIENCE" => (await _workExperiencesSvc.GetByIdAsync(id, ct))?.PersonId,
                "TRAINING" => (await _trainingsSvc.GetByIdAsync(id, ct))?.PersonId,
                "LANGUAGE" => (await _languagesSvc.GetByIdAsync(id, ct))?.PersonId,
                "BOOK" => (await _booksSvc.GetByIdAsync(id, ct))?.PersonId,
                _ => null,
            };
        }

        private ObjectResult Forbid403(string message) => StatusCode(403, new
        {
            status = "error",
            error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
        });

        /// <summary>Lista todos los archivos (ojo: puede ser grande).</summary>
        [HttpGet]
        [RequirePermission("DOCUMENTS.READ")]
        public async Task<IActionResult> GetAll(CancellationToken ct) =>
            Ok(_mapper.Map<List<StoredFileDto>>(await _svc.GetAllAsync(ct)));

        /// <summary>Obtiene un archivo por ID (DB).</summary>
        [HttpGet("{id:int}")]
        [RequirePermission("DOCUMENTS.READ")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            var e = await _svc.GetByIdAsync(id, ct);
            return e is null ? NotFound() : Ok(_mapper.Map<StoredFileDto>(e));
        }

        /// <summary>Obtiene un archivo por GUID (recomendado para exponer en API).</summary>
        [HttpGet("guid/{fileGuid:guid}")]
        [RequirePermission("DOCUMENTS.READ")]
        public async Task<IActionResult> GetByGuid([FromRoute] Guid fileGuid, CancellationToken ct)
        {
            var e = await _svc.GetByGuidAsync(fileGuid, ct);
            return e is null ? NotFound() : Ok(_mapper.Map<StoredFileDto>(e));
        }

        /// <summary>
        /// Lista archivos por entidad (ej: contrato).
        /// Ej: /files/entity?directoryCode=HRCONTRACT&amp;entityType=CONTRACT&amp;entityId=987&amp;status=1
        /// </summary>
        [HttpGet("entity")]
        [RequirePermission("DOCUMENTS.READ")]
        public async Task<IActionResult> GetByEntity(
         [FromQuery] string directoryCode,
         [FromQuery] string entityType,
         [FromQuery] string entityId,
         [FromQuery] int? uploadYear,
         [FromQuery] int? status,
         CancellationToken ct)
        {
            _logger.LogInformation("GET /files/entity called: {DirectoryCode} {EntityType} {EntityId}",
                directoryCode, entityType, entityId);

            if (string.IsNullOrWhiteSpace(directoryCode) ||
                string.IsNullOrWhiteSpace(entityType) ||
                string.IsNullOrWhiteSpace(entityId))
            {
                _logger.LogWarning("BadRequest: missing required query params");
                return BadRequest("directoryCode, entityType y entityId son requeridos.");
            }

            if (!ElevatedRoles.Any(User.IsInRole))
            {
                var ownerPersonId = await ResolveHojaDeVidaOwnerPersonIdAsync(entityType, entityId, ct);
                if (ownerPersonId is not null && await _currentUser.GetPersonIdAsync(ct) != ownerPersonId)
                    return Forbid403("No puede consultar documentos de otra persona.");
            }

            var entities = await _svc.GetByEntityAsync(directoryCode, entityType, entityId, uploadYear, status, ct);
            return Ok(_mapper.Map<List<StoredFileDto>>(entities));
        }

        /// <summary>Crea un registro (metadata). Normalmente se usa junto al upload físico.</summary>
        [HttpPost]
        [RequirePermission("DOCUMENTS.CREATE")]
        public async Task<IActionResult> Create([FromBody] StoredFileCreateDto dto, CancellationToken ct)
        {
            var entityObj = _mapper.Map<StoredFile>(dto);

            // Defaults mínimos (si no lo seteas en mapper)
            if (entityObj.UploadYear == 0)
                entityObj.UploadYear = DateTime.Now.Year;

            var created = await _svc.CreateAsync(entityObj, ct);

            return CreatedAtAction(nameof(GetById), new { id = created.FileId }, _mapper.Map<StoredFileDto>(created));
        }

        /// <summary>Actualiza metadata.</summary>
        [HttpPut("{id:int}")]
        [RequirePermission("DOCUMENTS.UPDATE")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] StoredFileUpdateDto dto, CancellationToken ct)
        {
            var entityObj = _mapper.Map<StoredFile>(dto);
            await _svc.UpdateAsync(id, entityObj, ct);
            return NoContent();
        }

        /// <summary>Soft delete (Status=2) recomendado en vez de borrar físico.</summary>
        [HttpDelete("{id:int}")]
        [RequirePermission("DOCUMENTS.DELETE")]
        public async Task<IActionResult> SoftDelete([FromRoute] int id, CancellationToken ct)
        {
            // Si manejas usuario autenticado, aquí sacas el userId y lo pasas
            int? deletedBy = null;

            await _svc.SoftDeleteAsync(id, deletedBy, ct);
            return NoContent();
        }
    }
}

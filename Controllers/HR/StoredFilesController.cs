using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.StoredFile;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR
{
    [ApiController]
    [Route("storefiles")]
    public class StoredFilesController : ControllerBase
    {
        private readonly IStoredFileService _svc;
        private readonly IMapper _mapper;
        private readonly ILogger<StoredFilesController> _logger;

        public StoredFilesController(IStoredFileService svc, IMapper mapper, ILogger<StoredFilesController> logger)
        {
            _svc = svc;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>Lista todos los archivos (ojo: puede ser grande).</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct) =>
            Ok(_mapper.Map<List<StoredFileDto>>(await _svc.GetAllAsync(ct)));

        /// <summary>Obtiene un archivo por ID (DB).</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            var e = await _svc.GetByIdAsync(id, ct);
            return e is null ? NotFound() : Ok(_mapper.Map<StoredFileDto>(e));
        }

        /// <summary>Obtiene un archivo por GUID (recomendado para exponer en API).</summary>
        [HttpGet("guid/{fileGuid:guid}")]
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

            var entities = await _svc.GetByEntityAsync(directoryCode, entityType, entityId, uploadYear, status, ct);
            return Ok(_mapper.Map<List<StoredFileDto>>(entities));
        }

        /// <summary>Crea un registro (metadata). Normalmente se usa junto al upload físico.</summary>
        [HttpPost]
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
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] StoredFileUpdateDto dto, CancellationToken ct)
        {
            var entityObj = _mapper.Map<StoredFile>(dto);
            await _svc.UpdateAsync(id, entityObj, ct);
            return NoContent();
        }

        /// <summary>Soft delete (Status=2) recomendado en vez de borrar físico.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> SoftDelete([FromRoute] int id, CancellationToken ct)
        {
            // Si manejas usuario autenticado, aquí sacas el userId y lo pasas
            int? deletedBy = null;

            await _svc.SoftDeleteAsync(id, deletedBy, ct);
            return NoContent();
        }
    }
}

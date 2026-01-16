// WsUtaSystem.Controllers.DocumentsController.cs
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Documents;
using WsUtaSystem.Application.DTOs.StoredFile;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers
{
    [ApiController]
    [Route("documents")]
    public sealed class DocumentsController : ControllerBase
    {
        private readonly IDocumentOrchestratorService _orchestrator;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentsController> _logger;
        private readonly ICurrentUserService _currentUserService;

        public DocumentsController(
            IDocumentOrchestratorService orchestrator,
            IMapper mapper,
            ILogger<DocumentsController> logger,
            ICurrentUserService currentUserService)
        {
            _orchestrator = orchestrator;
            _mapper = mapper;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        // (1) MISMO TIPO PARA VARIOS ARCHIVOS (BATCH)
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> Upload([FromForm] DocumentUploadRequestDto request, CancellationToken ct)
        {
            var res = await _orchestrator.UploadAndRegisterAsync(request, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        // (2) TIPOS DIFERENTES PARA CADA ARCHIVO (MAPPED BATCH)
        [HttpPost("upload-mapped")]
        [Consumes("multipart/form-data")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> UploadMapped([FromForm] DocumentUploadMappedRequestDto request, CancellationToken ct)
        {
            var res = await _orchestrator.UploadMappedAndRegisterAsync(request, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        // (3) 1 ARCHIVO (SINGLE)
        [HttpPost("upload-single")]
        [Consumes("multipart/form-data")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> UploadSingle([FromForm] DocumentUploadSingleRequestDto request, CancellationToken ct)
        {
            var res = await _orchestrator.UploadSingleAndRegisterAsync(request, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        /// <summary>
        /// Lista archivos por entidad (contrato, etc.)
        /// Importante: StoredFileDto debe incluir DocumentTypeId y opcional DocumentTypeName
        /// para visualizarlo en el frontend.
        /// </summary>
        [HttpGet("entity")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ListByEntity([FromQuery] DocumentListQueryDto q, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q.DirectoryCode) ||
                string.IsNullOrWhiteSpace(q.EntityType) ||
                string.IsNullOrWhiteSpace(q.EntityId))
            {
                return BadRequest("DirectoryCode, EntityType y EntityId son requeridos.");
            }

            var entities = await _orchestrator.ListByEntityAsync(
                q.DirectoryCode,
                q.EntityType,
                q.EntityId,
                q.UploadYear,
                q.Status,
                ct
            );

            var dto = _mapper.Map<List<StoredFileDto>>(entities);
            return Ok(dto);
        }

        /// <summary>
        /// Descarga por GUID (recomendado para exponer hacia el cliente).
        /// </summary>
        [HttpGet("download/{fileGuid:guid}")]
        public async Task<IActionResult> Download([FromRoute] Guid fileGuid, CancellationToken ct)
        {
            var file = await _orchestrator.DownloadByGuidAsync(fileGuid, ct);
            if (file is null) return NotFound(new { success = false, message = "Archivo no encontrado." });

            return File(file.Value.fileBytes, file.Value.contentType, file.Value.fileName);
        }

        /// <summary>
        /// Elimina: soft delete en DB y opcional borrar físico.
        /// </summary>
        [HttpDelete("{fileGuid:guid}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid fileGuid,
            [FromQuery] bool deletePhysical = false,
            CancellationToken ct = default)
        {
            int? deletedBy = _currentUserService.EmployeeId;
            _logger.LogInformation("User {UserId} is deleting file {FileGuid}, deletePhysical={DeletePhysical}, employeid {_currentUserService.EmployeeId}",
                deletedBy?.ToString() ?? "Anonymous",
                fileGuid,
                deletePhysical,
                _currentUserService.EmployeeId);
            var ok = await _orchestrator.DeleteByGuidAsync(fileGuid, deletePhysical, deletedBy, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}

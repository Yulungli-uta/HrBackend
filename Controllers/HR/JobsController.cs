using AutoMapper;
using WsUtaSystem.Application.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Jobs;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR
{
    [ApiController]
    [Route("jobs")]
    public class JobsController : ControllerBase
    {
        private readonly IJobService _svc;
        private readonly IOccupationalGroupService _groups;
        private readonly IMapper _mapper;

        public JobsController(IJobService svc, IOccupationalGroupService groups, IMapper mapper)
        {
            _svc = svc;
            _groups = groups;
            _mapper = mapper;
        }

        /// <summary>Lista todos los registros de Jobs.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct) =>
            Ok(_mapper.Map<List<JobDto>>(await _svc.GetAllAsync(ct)));

        /// <summary>Obtiene un registro por ID.</summary>
        /// <param name="id">Identificador</param>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            var e = await _svc.GetByIdAsync(id, ct);
            return e is null ? NotFound() : Ok(_mapper.Map<JobDto>(e));
        }

        /// <summary>Retorna un resultado paginado de registros de Jobs.</summary>
        /// <param name="page">Número de página (base 1).</param>
        /// <param name="pageSize">Cantidad de registros por página. Máximo 200.</param>
        /// <param name="search">Texto de búsqueda por título.</param>
        /// <param name="sortBy">Campo de ordenamiento (opcional).</param>
        /// <param name="sortDirection">Dirección del orden: asc | desc (opcional).</param>
        /// <param name="familyTypeId">Grupo ocupacional (ref_Types OCCUPATIONAL_GROUP_FAMILY) de la escala del cargo.</param>
        /// <param name="withoutGroup">true = solo cargos sin escala asignada.</param>
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = "asc",
            [FromQuery] bool? isActive = null,
            [FromQuery] int? groupId = null,
            [FromQuery] int? jobTypeId = null,
            [FromQuery] int? uepScaleTypeId = null,
            [FromQuery] int? familyTypeId = null,
            [FromQuery] bool? withoutGroup = null,
            CancellationToken ct = default)
        {
            (page, pageSize) = CatalogPredicates.NormalizePaging(page, pageSize);

            // Busca por palabras sin importar mayúsculas ni tildes; filtros opcionales por estado, grupo ocupacional y tipo (régimen).
            var predicate = CatalogPredicates.ByWords<Job>(search,
                w => j => j.Description != null && EF.Functions.Collate(j.Description, CatalogPredicates.Collation).Contains(w));
            Expression<Func<Job, bool>>? byState = isActive.HasValue ? j => j.IsActive == isActive.Value : null;
            Expression<Func<Job, bool>>? byGroup = groupId.HasValue ? j => j.GroupId == groupId.Value : null;
            Expression<Func<Job, bool>>? byType = jobTypeId.HasValue ? j => j.JobTypeId == jobTypeId.Value : null;
            // Clasificación adicional (Escala UEP): la tiene el grupo ocupacional, así que se buscan los grupos con esa clasificación
            // (catálogo corto) y se filtran los cargos que usan alguno de ellos.
            Expression<Func<Job, bool>>? byUep = null;
            if (uepScaleTypeId.HasValue)
            {
                var uepGroups = await _groups.GetPagedAsync(g => g.UepScaleTypeId == uepScaleTypeId.Value, 1, 200, ct);
                var groupIds = uepGroups.Items.Select(g => (int?)g.GroupId).ToList();
                byUep = j => groupIds.Contains(j.GroupId);
            }
            // Grupo ocupacional (familia): cargos cuya escala pertenece a ese grupo; withoutGroup = cargos sin escala asignada.
            Expression<Func<Job, bool>>? byFamily = null;
            if (familyTypeId.HasValue)
            {
                var familyGroups = await _groups.GetPagedAsync(g => g.FamilyTypeId == familyTypeId.Value, 1, 200, ct);
                var familyGroupIds = familyGroups.Items.Select(g => (int?)g.GroupId).ToList();
                byFamily = j => familyGroupIds.Contains(j.GroupId);
            }
            Expression<Func<Job, bool>>? byUnclassified = withoutGroup == true ? j => j.GroupId == null : null;
            var filter = predicate.And(byState).And(byGroup).And(byType).And(byUep).And(byFamily).And(byUnclassified);

            // Orden por descripción solo si se pide (sortBy=description); sin sortBy se conserva el orden previo del endpoint.
            var byDescription = string.Equals(sortBy, "description", StringComparison.OrdinalIgnoreCase);
            var ascending = !string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var pagedEntities = byDescription
                ? await _svc.GetPagedAsync(filter, page, pageSize, ct, orderBy: j => j.Description!, ascending: ascending)
                : await _svc.GetPagedAsync(filter, page, pageSize, ct);

            var dtoItems = _mapper.Map<List<JobDto>>(pagedEntities.Items);

            return Ok(new
            {
                items = dtoItems,
                page = pagedEntities.Page,
                pageSize = pagedEntities.PageSize,
                totalCount = pagedEntities.TotalCount,
                totalPages = pagedEntities.TotalPages,
                hasPreviousPage = pagedEntities.HasPreviousPage,
                hasNextPage = pagedEntities.HasNextPage
            });
        }

        /// <summary>Crea un nuevo registro.</summary>
        [HttpPost]
        [RequirePermission("CATALOGS.CREATE")]
        public async Task<IActionResult> Create([FromBody] CreateJobDto dto, CancellationToken ct)
        {
            var entityObj = _mapper.Map<Job>(dto);
            var created = await _svc.CreateAsync(entityObj, ct);
            var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
            return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<JobDto>(created));
        }

        /// <summary>Actualiza un registro existente.</summary>
        [HttpPut("{id:int}")]
        [RequirePermission("CATALOGS.UPDATE")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateJobDto dto, CancellationToken ct)
        {
            var entityObj = _mapper.Map<Job>(dto);
            await _svc.UpdateAsync(id, entityObj, ct);
            return NoContent();
        }

        /// <summary>Elimina un registro por ID.</summary>
        [HttpDelete("{id:int}")]
        [RequirePermission("CATALOGS.DELETE")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>Obtiene todos los trabajos activos.</summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveJobs(CancellationToken ct) =>
            Ok(_mapper.Map<List<JobDto>>(await _svc.GetActiveJobsAsync(ct)));

        /// <summary>Busca trabajos por título.</summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchJobs([FromQuery] string title, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest("Title parameter is required");

            var jobs = await _svc.SearchJobsByTitleAsync(title, ct);
            return Ok(_mapper.Map<List<JobDto>>(jobs));
        }
    }
}

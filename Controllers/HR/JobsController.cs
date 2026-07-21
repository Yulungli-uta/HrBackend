using AutoMapper;
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
        private readonly IMapper _mapper;

        public JobsController(IJobService svc, IMapper mapper)
        {
            _svc = svc;
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
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = "asc",
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            System.Linq.Expressions.Expression<Func<Job, bool>>? predicate = null;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                predicate = j =>
                    j.Description != null && j.Description.Contains(term);
            }

            var pagedEntities = predicate is not null
                ? await _svc.GetPagedAsync(predicate, page, pageSize, ct)
                : await _svc.GetPagedAsync(page, pageSize, ct);

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

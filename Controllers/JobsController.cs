using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Jobs;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers
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

        /// <summary>Crea un nuevo registro.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateJobDto dto, CancellationToken ct)
        {
            var entityObj = _mapper.Map<Job>(dto);
            var created = await _svc.CreateAsync(entityObj, ct);
            var idVal = created?.GetType()?.GetProperties()?.FirstOrDefault(p => p.Name.Equals("Id") || p.Name.EndsWith("Id") || p.Name.EndsWith("ID"))?.GetValue(created);
            return CreatedAtAction(nameof(GetById), new { id = idVal }, _mapper.Map<JobDto>(created));
        }

        /// <summary>Actualiza un registro existente.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateJobDto dto, CancellationToken ct)
        {
            var entityObj = _mapper.Map<Job>(dto);
            await _svc.UpdateAsync(id, entityObj, ct);
            return NoContent();
        }

        /// <summary>Elimina un registro por ID.</summary>
        [HttpDelete("{id:int}")]
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

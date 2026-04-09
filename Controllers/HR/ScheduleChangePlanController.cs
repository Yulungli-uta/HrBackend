using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.ScheduleChange;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR
{
    [ApiController]
    [Route("/schedule-change-plans")]
    public class ScheduleChangePlanController : ControllerBase
    {
        private readonly IScheduleChangePlanService _service;
        private readonly ILogger<ScheduleChangePlanController> _logger;

        public ScheduleChangePlanController(
            IScheduleChangePlanService service,
            ILogger<ScheduleChangePlanController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>Retorna planes paginados.</summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _service.GetPagedAsync(page, pageSize, ct);
            return Ok(result);
        }

        /// <summary>Retorna un plan por ID.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var plan = await _service.GetByIdAsync(id, ct);
            return plan is not null ? Ok(plan) : NotFound();
        }

        /// <summary>Retorna los planes creados por un jefe inmediato.</summary>
        [HttpGet("boss/{bossId:int}")]
        public async Task<IActionResult> GetByBoss(int bossId, CancellationToken ct = default)
        {
            var plans = await _service.GetByBossIdAsync(bossId, ct);
            return Ok(plans);
        }

        /// <summary>Retorna planes filtrados por estado.</summary>
        [HttpGet("status/{statusTypeId:int}")]
        public async Task<IActionResult> GetByStatus(int statusTypeId, CancellationToken ct = default)
        {
            var plans = await _service.GetByStatusAsync(statusTypeId, ct);
            return Ok(plans);
        }

        /// <summary>Crea una nueva planificación de cambio de horario.</summary>
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateScheduleChangePlanRequest request,
            CancellationToken ct = default)
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.PlanID }, created);
        }

        /// <summary>Aprueba o rechaza un plan pendiente.</summary>
        [HttpPatch("{id:int}/approve")]
        public async Task<IActionResult> Approve(
            int id,
            [FromBody] ApproveScheduleChangePlanRequest request,
            CancellationToken ct = default)
        {
            if (id != request.PlanID) return BadRequest("El ID del plan no coincide.");
            await _service.ApproveAsync(request, ct);
            return NoContent();
        }

        /// <summary>Cancela un plan antes de su ejecución.</summary>
        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(
            int id,
            [FromBody] CancelScheduleChangePlanRequest request,
            CancellationToken ct = default)
        {
            if (id != request.PlanID) return BadRequest("El ID del plan no coincide.");
            await _service.CancelAsync(request, ct);
            return NoContent();
        }
    }
}

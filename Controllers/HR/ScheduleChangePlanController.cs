using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.ScheduleChange;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR
{
    [ApiController]
    [Route("/schedule-change-plans")]
    public class ScheduleChangePlanController : ControllerBase
    {
        private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA", "Supervisor" };

        private readonly IScheduleChangePlanService _service;
        private readonly ILogger<ScheduleChangePlanController> _logger;
        private readonly ICurrentUserService _currentUser;

        public ScheduleChangePlanController(
            IScheduleChangePlanService service,
            ILogger<ScheduleChangePlanController> logger,
            ICurrentUserService currentUser)
        {
            _service = service;
            _logger = logger;
            _currentUser = currentUser;
        }

        /// <summary>Retorna planes paginados.</summary>
        [HttpGet]
        [RequirePermission("SCHEDULE_CHANGE_PLANS.READ")]
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
        [RequirePermission("SCHEDULE_CHANGE_PLANS.READ")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var plan = await _service.GetByIdAsync(id, ct);
            if (plan is null) return NotFound();

            if (_currentUser.EmployeeId != plan.RequestedByBossID && !ElevatedRoles.Any(User.IsInRole))
                return Forbid403("No puede consultar un plan de otro jefe.");

            return Ok(plan);
        }

        /// <summary>Retorna los planes creados por un jefe inmediato.</summary>
        [HttpGet("boss/{bossId:int}")]
        [RequirePermission("SCHEDULE_CHANGE_PLANS.READ")]
        public async Task<IActionResult> GetByBoss(int bossId, CancellationToken ct = default)
        {
            if (_currentUser.EmployeeId != bossId && !ElevatedRoles.Any(User.IsInRole))
                return Forbid403("No puede consultar los planes de otro jefe.");

            var plans = await _service.GetByBossIdAsync(bossId, ct);
            return Ok(plans);
        }

        /// <summary>Retorna planes filtrados por estado.</summary>
        [HttpGet("status/{statusTypeId:int}")]
        [RequirePermission("SCHEDULE_CHANGE_PLANS.READ")]
        public async Task<IActionResult> GetByStatus(int statusTypeId, CancellationToken ct = default)
        {
            var plans = await _service.GetByStatusAsync(statusTypeId, ct);
            return Ok(plans);
        }

        /// <summary>Crea una nueva planificación de cambio de horario.</summary>
        [HttpPost]
        [RequirePermission("SCHEDULE_CHANGE_PLANS.CREATE")]
        public async Task<IActionResult> Create(
            [FromBody] CreateScheduleChangePlanRequest request,
            CancellationToken ct = default)
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.PlanID }, created);
        }

        /// <summary>Aprueba o rechaza un plan pendiente.</summary>
        [HttpPatch("{id:int}/approve")]
        [RequirePermission("SCHEDULE_CHANGE_PLANS.APPROVE")]
        public async Task<IActionResult> Approve(
            int id,
            [FromBody] ApproveScheduleChangePlanRequest request,
            CancellationToken ct = default)
        {
            if (id != request.PlanID) return BadRequest("El ID del plan no coincide.");

            var plan = await _service.GetByIdAsync(id, ct);
            if (plan is null) return NotFound();

            if (_currentUser.EmployeeId != plan.RequestedByBossID && !ElevatedRoles.Any(User.IsInRole))
                return Forbid403("No puede aprobar un plan de otro jefe.");

            await _service.ApproveAsync(request, ct);
            return NoContent();
        }

        /// <summary>Cancela un plan antes de su ejecución.</summary>
        [HttpPatch("{id:int}/cancel")]
        [RequirePermission("SCHEDULE_CHANGE_PLANS.CANCEL")]
        public async Task<IActionResult> Cancel(
            int id,
            [FromBody] CancelScheduleChangePlanRequest request,
            CancellationToken ct = default)
        {
            if (id != request.PlanID) return BadRequest("El ID del plan no coincide.");

            var plan = await _service.GetByIdAsync(id, ct);
            if (plan is null) return NotFound();

            if (_currentUser.EmployeeId != plan.RequestedByBossID && !ElevatedRoles.Any(User.IsInRole))
                return Forbid403("No puede cancelar un plan de otro jefe.");

            await _service.CancelAsync(request, ct);
            return NoContent();
        }

        private ObjectResult Forbid403(string message) => StatusCode(403, new
        {
            status = "error",
            error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
        });
    }
}

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.TimePlanning;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers
{
    [ApiController]
    [Route("planning/timePlannings")]
    public class TimePlanningsController : ControllerBase
    {
        private readonly ITimePlanningService _svc;
        private readonly IMapper _mapper;

        public TimePlanningsController(ITimePlanningService svc, IMapper mapper)
        {
            _svc = svc;
            _mapper = mapper;
        }

        /// <summary>Lista todas las planificaciones.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct) =>
            Ok(_mapper.Map<List<TimePlanningResponseDTO>>(await _svc.GetAllAsync(ct)));

        /// <summary>Obtiene una planificación por ID.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            var planning = await _svc.GetByIdAsync(id, ct);
            return planning is null ? NotFound() : Ok(_mapper.Map<TimePlanningResponseDTO>(planning));
        }

        /// <summary>Obtiene planificaciones por empleado.</summary>
        [HttpGet("employee/{employeeId:int}")]
        public async Task<IActionResult> GetByEmployee([FromRoute] int employeeId, CancellationToken ct) =>
            Ok(_mapper.Map<List<TimePlanningResponseDTO>>(await _svc.GetByEmployeeAsync(employeeId, ct)));

        /// <summary>Obtiene planificaciones por estado.</summary>
        [HttpGet("status/{statusTypeId:int}")]
        public async Task<IActionResult> GetByStatus([FromRoute] int statusTypeId, CancellationToken ct) =>
            Ok(_mapper.Map<List<TimePlanningResponseDTO>>(await _svc.GetByStatusAsync(statusTypeId, ct)));

        /// <summary>Busca planificaciones con filtros.</summary>
        //[HttpGet("search")]
        //public async Task<IActionResult> Search([FromQuery] TimePlanningSearchDto searchDto, CancellationToken ct) =>
        //    Ok(await _svc.SearchAsync(searchDto, ct));

        /// <summary>Crea una nueva planificación.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TimePlanningCreateDTO dto, CancellationToken ct)
        {
            //Console.WriteLine($"************varlores recibidos timePlannings {dto.ToString}");
            var planning = _mapper.Map<TimePlanning>(dto);
            Console.WriteLine($"📥 DTO recibido: {System.Text.Json.JsonSerializer.Serialize(dto)}");
            Console.WriteLine($"PlanStatusTypeID recibido: {dto.PlanStatusTypeID}");
            var created = await _svc.CreateAsync(planning, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.PlanID }, _mapper.Map<TimePlanningResponseDTO>(created));
        }

        /// <summary>Actualiza una planificación existente.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TimePlanningCreateDTO dto, CancellationToken ct)
        {
            var planning = _mapper.Map<TimePlanning>(dto);
            await _svc.UpdateAsync(id, planning, ct);
            return NoContent();
        }

        /// <summary>Elimina una planificación por ID.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>Envía una planificación para aprobación.</summary>
        //[HttpPost("{id:int}/submit")]
        //public async Task<IActionResult> SubmitForApproval([FromRoute] int id, [FromBody] TimePlanningCreateDTO dto, CancellationToken ct)
        //{
        //    var planning = await _svc.SubmitForApprovalAsync(id, dto.SubmittedBy, ct);
        //    return Ok(_mapper.Map<TimePlanningResponseDTO>(planning));
        //}

        ///// <summary>Aprobar una planificación.</summary>
        //[HttpPost("{id:int}/approve")]
        //public async Task<IActionResult> Approve([FromRoute] int id, [FromBody] ApprovePlanningDto dto, CancellationToken ct)
        //{
        //    var planning = await _svc.ApprovePlanningAsync(id, dto.ApprovedBy, dto.SecondApprover, ct);
        //    return Ok(_mapper.Map<TimePlanningDto>(planning));
        //}

        /// <summary>Rechazar una planificación.</summary>
        //[HttpPost("{id:int}/reject")]
        //public async Task<IActionResult> Reject([FromRoute] int id, [FromBody] TimePlanningCreateDTO dto, CancellationToken ct)
        //{
        //    var planning = await _svc.RejectPlanningAsync(id, dto.RejectedBy, dto.Reason, ct);
        //    return Ok(_mapper.Map<TimePlanningDto>(planning));
        //}
    }
}

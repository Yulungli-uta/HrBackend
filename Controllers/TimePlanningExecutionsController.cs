using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.TimePlanningExecution;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers
{
    [ApiController]
    [Route("planning/{planEmployeeId:int}/executions")]
    public class TimePlanningExecutionsController : ControllerBase
    {
        private readonly ITimePlanningExecutionService _svc;
        private readonly IMapper _mapper;

        public TimePlanningExecutionsController(ITimePlanningExecutionService svc, IMapper mapper)
        {
            _svc = svc;
            _mapper = mapper;
        }

        /// <summary>Lista todas las ejecuciones de un empleado en planificación.</summary>
        [HttpGet]
        public async Task<IActionResult> GetByPlanEmployee([FromRoute] int planEmployeeId, CancellationToken ct) =>
            Ok(_mapper.Map<List<TimePlanningExecutionResponseDTO>>(await _svc.GetByPlanEmployeeIdAsync(planEmployeeId, ct)));

        /// <summary>Obtiene una ejecución por ID.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int planEmployeeId, [FromRoute] int id, CancellationToken ct)
        {
            var execution = await _svc.GetByIdAsync(id, ct);
            return execution is null ? NotFound() : Ok(_mapper.Map<TimePlanningExecutionResponseDTO>(execution));
        }

        /// <summary>Registra tiempo de trabajo.</summary>
        [HttpPost]
        public async Task<IActionResult> RegisterWorkTime([FromRoute] int planEmployeeId, [FromBody] TimePlanningExecutionCreateDTO dto, CancellationToken ct)
        {
            dto.PlanEmployeeID = planEmployeeId; // Asegurar coincidencia con la ruta
            var execution = _mapper.Map<TimePlanningExecution>(dto);
            var created = await _svc.CreateAsync(execution, ct);
            return CreatedAtAction(nameof(GetById), new { planEmployeeId, id = created.ExecutionID }, _mapper.Map<TimePlanningExecutionResponseDTO>(created));
        }

        /// <summary>Actualiza una ejecución.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int planEmployeeId, [FromRoute] int id, [FromBody] TimePlanningExecutionUpdateDTO dto, CancellationToken ct)
        {
            var execution = _mapper.Map<TimePlanningExecution>(dto);
            await _svc.UpdateAsync(id, execution, ct);
            return NoContent();
        }

        /// <summary>Elimina una ejecución.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int planEmployeeId, [FromRoute] int id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }

        ///// <summary>Verifica una ejecución.</summary>
        //[HttpPost("{id:int}/verify")]
        //public async Task<IActionResult> Verify([FromRoute] int planEmployeeId, [FromRoute] int id, [FromBody] TimePlanningExecutionCreateDTO dto, CancellationToken ct)
        //{
        //    var execution = await _svc.VerifyExecutionAsync(id, dto.VerifiedBy, dto.Comments, ct);
        //    return Ok(_mapper.Map<TimePlanningExecutionResponseDTO>(execution));
        //}
    }
}

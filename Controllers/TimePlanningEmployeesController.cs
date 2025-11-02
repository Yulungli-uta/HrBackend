using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;
using WsUtaSystem.Application.DTOs.TimePlanningEmployee;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers
{

    [ApiController]
    [Route("planning/employees")]
    public class TimePlanningEmployeesController : ControllerBase
    {
        private readonly ITimePlanningEmployeeService _svc;
        private readonly IMapper _mapper;
        private readonly ILogger<TimePlanningEmployeesController> _logger;

        public TimePlanningEmployeesController(ITimePlanningEmployeeService svc, IMapper mapper)
        {
            _svc = svc;
            _mapper = mapper;
        }

        /// <summary>Lista todos los empleados de una planificación.</summary>
        [HttpGet("by-plan/{planId:int}")]
        public async Task<IActionResult> GetByPlan([FromRoute] int planId, CancellationToken ct)
        {
            Console.WriteLine($"GetByPlan llamado con planId: {planId}");

            if (planId <= 0)
            {
                return BadRequest("El PlanID debe ser mayor que 0");
            }

            var result = await _svc.GetByPlanIdAsync(planId, ct);
            Console.WriteLine($"Resultado: {result?.Count()} registros");

            return Ok(result);
        }
        //public async Task<IActionResult> GetByPlan([FromRoute] int planId, CancellationToken ct) =>
        //    Ok(_mapper.Map<List<TimePlanningEmployeeResponseDTO>>(await _svc.GetByPlanIdAsync(planId, ct)));
        //public async Task<IActionResult> GetByPlan([FromRoute] int planId, CancellationToken ct) { 
        //    Console.WriteLine($"GetByPlan llamado con planId: {planId}");
        //    var result = await _svc.GetByPlanIdAsync(planId, ct);
        //    Console.WriteLine($"Resultado: {result?.Count()} registros");
        //    return Ok(_mapper.Map<List<TimePlanningEmployeeResponseDTO>>(result));
        //}

        /// <summary>Obtiene un empleado de planificación por ID.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int planId, [FromRoute] int id, CancellationToken ct)
        {
            var planningEmployee = await _svc.GetByIdAsync(id, ct);
            return planningEmployee is null ? NotFound() : Ok(_mapper.Map<TimePlanningEmployeeResponseDTO>(planningEmployee));
        }

        /// <summary>Obtiene empleados de planificación por estado.</summary>
        //[HttpGet("status/{statusTypeId:int}")]
        //public async Task<IActionResult> GetByStatus([FromRoute] int planId, [FromRoute] int statusTypeId, CancellationToken ct) =>
        //    Ok(_mapper.Map<List<TimePlanningEmployeeResponseDTO>>(await _svc.GetByStatusAsync(statusTypeId, ct)));

        /// <summary>Agrega un empleado a la planificación.</summary>
        [HttpPost]
        public async Task<IActionResult> AddEmployee([FromRoute] int planId, [FromBody] TimePlanningEmployeeCreateDTO dto, CancellationToken ct)
        {
            Console.WriteLine($"AddEmployee llamado con planId: {planId} empleados: {dto.PlanID}");
           // dto.PlanID = planId; // Asegurar que el PlanID coincide con la ruta
            var planningEmployee = _mapper.Map<TimePlanningEmployee>(dto);
            Console.WriteLine($"PlanningEmployee mapeado: {planningEmployee.PlanID} " +
                $"empl: {planningEmployee.EmployeeID} status: {planningEmployee.EmployeeStatusTypeID} ");
            var created = await _svc.CreateAsync(planningEmployee, ct);
            return CreatedAtAction(nameof(GetById), new { planId, id = created.PlanEmployeeID }, _mapper.Map<TimePlanningEmployeeResponseDTO>(created));
        }

        /// <summary>Actualiza un empleado en la planificación.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int planId, [FromRoute] int id, [FromBody] TimePlanningEmployeeUpdateDTO dto, CancellationToken ct)
        {
            var planningEmployee = _mapper.Map<TimePlanningEmployee>(dto);
            await _svc.UpdateAsync(id, planningEmployee, ct);
            return NoContent();
        }

        /// <summary>Elimina un empleado de la planificación.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int planId, [FromRoute] int id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>Actualiza el estado de un empleado en la planificación.</summary>
        //[HttpPatch("{id:int}/status")]
        //public async Task<IActionResult> UpdateStatus([FromRoute] int planId, [FromRoute] int id, [FromBody] TimePlanningEmployeeUpdateDTO dto, CancellationToken ct)
        //{
        //    var planningEmployee = await _svc.UpdateEmployeeStatusAsync(id, dto.StatusTypeID, ct);
        //    return Ok(_mapper.Map<TimePlanningEmployeeResponseDTO>(planningEmployee));
        //}
    }
}

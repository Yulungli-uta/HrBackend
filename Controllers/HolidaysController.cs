using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Holiday;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers
{
    [ApiController]
    [Route("holiday")]
    public class HolidaysController : ControllerBase
    {
        private readonly IHolidayService _svc;
        private readonly IMapper _mapper;

        public HolidaysController(IHolidayService svc, IMapper mapper)
        {
            _svc = svc;
            _mapper = mapper;
        }

        /// <summary>Lista todos los feriados.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct) =>
            Ok(_mapper.Map<List<HolidayResponseDTO>>(await _svc.GetAllAsync(ct)));

        /// <summary>Obtiene un feriado por ID.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            var holiday = await _svc.GetByIdAsync(id, ct);
            return holiday is null ? NotFound() : Ok(_mapper.Map<HolidayResponseDTO>(holiday));
        }

        /// <summary>Obtiene feriados por año.</summary>
        [HttpGet("year/{year:int}")]
        public async Task<IActionResult> GetByYear([FromRoute] int year, CancellationToken ct) =>
            Ok(_mapper.Map<List<HolidayResponseDTO>>(await _svc.GetByYearAsync(year, ct)));

        /// <summary>Obtiene feriados activos.</summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActive(CancellationToken ct) =>
            Ok(_mapper.Map<List<HolidayResponseDTO>>(await _svc.GetActiveHolidaysAsync(ct)));

        /// <summary>Verifica si una fecha es feriado.</summary>
        [HttpGet("check/{date:datetime}")]
        public async Task<IActionResult> IsHoliday([FromRoute] DateTime date, CancellationToken ct) =>
            Ok(await _svc.IsHolidayAsync(date, ct));

        /// <summary>Crea un nuevo feriado.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] HolidayResponseDTO dto, CancellationToken ct)
        {
            var holiday = _mapper.Map<Holiday>(dto);
            var created = await _svc.CreateAsync(holiday, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.HolidayID }, _mapper.Map<HolidayResponseDTO>(created));
        }

        /// <summary>Actualiza un feriado existente.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] HolidayCreateDTO dto, CancellationToken ct)
        {
            var holiday = _mapper.Map<Holiday>(dto);
            await _svc.UpdateAsync(id, holiday, ct);
            return NoContent();
        }

        /// <summary>Elimina un feriado por ID.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
    }
}

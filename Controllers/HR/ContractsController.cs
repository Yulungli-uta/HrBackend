using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.ContractRequest;
using WsUtaSystem.Application.DTOs.Contracts;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers.HR;

[ApiController]
[Route("contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractsService _service;
    private readonly IMapper _mapper;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(_mapper.Map<List<ContractsDto>>(await _service.GetAllAsync(ct)));

    public ContractsController(IContractsService service, IMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var entity = await _service.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        return Ok(_mapper.Map<ContractsDto>(entity));
    }

    [HttpPost]
    public async Task<IActionResult> Create(ContractsCreateDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<Contracts>(dto);

        var created = await _service.CreateAsync(entity, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.ContractID },
            _mapper.Map<ContractsDto>(created)
        );
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        ContractsUpdateDto dto,
        CancellationToken ct)
    {
        if (dto.ContractID != 0 && dto.ContractID != id)
            return BadRequest("ContractID no coincide con la ruta.");

        // ⚠️ NO se mapea entidad aquí
        // ⚠️ El Service ya sabe cómo actualizar campo por campo
        await _service.UpdateAsync(id, dto, ct);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("status/allowed")]
    public async Task<IActionResult> Allowed([FromQuery] int currentStatusTypeId, CancellationToken ct)
    {
        var next = await _service.GetAllowedNextStatusesAsync(currentStatusTypeId, ct);
        return Ok(next);
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ContractChangeStatusDto dto, CancellationToken ct)
    {
        await _service.ChangeStatusAsync(id, dto.ToStatusTypeID, dto.Comment, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> History(int id, CancellationToken ct)
    {
        var items = await _service.GetStatusHistoryAsync(id, ct);
        return Ok(items);
    }

    [HttpGet("{id:int}/addendums")]
    public async Task<IActionResult> Addendums(int id, CancellationToken ct)
    {
        var items = await _service.GetAddendumsAsync(id, ct);
        return Ok(items);
    }

}

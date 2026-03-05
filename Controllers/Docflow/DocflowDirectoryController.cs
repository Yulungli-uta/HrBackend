using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Docflow;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.Docflow;

/// <summary>
/// Controlador para obtener catálogos y directorios en Docflow.
/// Solo orquesta llamadas al servicio, sin lógica de negocio.
/// </summary>
[ApiController]
[Route("directory")]
[Produces("application/json")]
public sealed class DocflowDirectoryController : ControllerBase
{
    private readonly IDocflowDirectoryService _directoryService;

    public DocflowDirectoryController(IDocflowDirectoryService directoryService)
    {
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
    }

    /// <summary>
    /// Obtiene los parámetros de un catálogo específico.
    /// </summary>
    /// <param name="type">Tipo de catálogo (instance_statuses, movement_types, priorities, areas, users)</param>
    /// <param name="ct">Token de cancelación</param>
    /// <returns>Lista de parámetros del catálogo solicitado</returns>
    [HttpGet("{type}")]
    [ProducesResponseType(typeof(IEnumerable<DirectoryParameterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDirectoryByType(
        [FromRoute] string type,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _directoryService.GetDirectoryByTypeAsync(type, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Error al obtener el catálogo", error = ex.Message });
        }
    }
}
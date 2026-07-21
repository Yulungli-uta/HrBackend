using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>
/// Perfil académico completo de un docente (experiencia, publicaciones, capacitaciones,
/// investigación, tesis doctorales, idiomas, evaluación de desempeño), para procesos
/// de validación, promoción o evaluación académica.
/// </summary>
[ApiController]
[Route("academic-promotion/teachers")]
public sealed class AcademicPromotionController : ControllerBase
{
    private readonly IAcademicPromotionService _service;
    private readonly ILogger<AcademicPromotionController> _logger;

    public AcademicPromotionController(
        IAcademicPromotionService service,
        ILogger<AcademicPromotionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Perfil académico del docente, buscado por su identificación (cédula).</summary>
    /// <param name="identification">Cédula del docente.</param>
    [HttpGet("{identification}")]
    [RequirePermission("EMPLOYEES.READ")]
    public async Task<IActionResult> GetProfile([FromRoute] string identification, CancellationToken ct)
    {
        if (!await _service.IsCurrentUserAuthorizedAsync(ct))
            return Forbid();

        if (string.IsNullOrWhiteSpace(identification))
            return BadRequest("La identificación es requerida.");

        var masked = identification.Length > 4 ? new string('*', identification.Length - 4) + identification[^4..] : identification;
        _logger.LogInformation("Consulta de perfil académico docente. Identificación={Masked}", masked);

        var profile = await _service.GetProfileByIdentificationAsync(identification.Trim(), ct);
        if (profile is null) return NotFound();

        return Ok(profile);
    }
}

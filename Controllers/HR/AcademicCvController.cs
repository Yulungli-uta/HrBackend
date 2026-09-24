using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;
using WsUtaSystem.Reports.Abstractions;

namespace WsUtaSystem.Controllers.HR;

/// <summary>Exportación de la Hoja de Vida Académica (formación, experiencia, publicaciones,
/// libros, capacitaciones, idiomas, áreas de conocimiento) en PDF. Deliberadamente excluye
/// datos personales sensibles — ver <see cref="Application.DTOs.AcademicCv.AcademicCvDto"/>.</summary>
[ApiController]
[Route("people/{personId:int}/academic-cv")]
public class AcademicCvController : ControllerBase
{
    private static readonly string[] ElevatedRoles = { "Administrador", "R_RH", "R_RH_ANALISTA", "R_RH_ESPECIALISTA" };

    private readonly IAcademicCvService _cvService;
    private readonly IAcademicCvPdfComposer _pdfComposer;
    private readonly ICurrentUserService _currentUser;

    public AcademicCvController(
        IAcademicCvService cvService,
        IAcademicCvPdfComposer pdfComposer,
        ICurrentUserService currentUser)
    {
        _cvService = cvService;
        _pdfComposer = pdfComposer;
        _currentUser = currentUser;
    }

    /// <summary>Genera y descarga el PDF de la Hoja de Vida Académica de una persona.</summary>
    [HttpGet("pdf")]
    [RequirePermission("EMPLOYEE_PROFILE.READ")]
    public async Task<IActionResult> GetPdf([FromRoute] int personId, CancellationToken ct)
    {
        if (!ElevatedRoles.Any(User.IsInRole) && await _currentUser.GetPersonIdAsync(ct) != personId)
            return Forbid403("No puede exportar la hoja de vida de otra persona.");

        var cv = await _cvService.BuildAsync(personId, ct);
        if (cv is null) return NotFound();

        var pdfBytes = _pdfComposer.Compose(cv);
        var fileName = $"HojaDeVidaAcademica_{cv.IdCard}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    private ObjectResult Forbid403(string message) => StatusCode(403, new
    {
        status = "error",
        error = new { code = "FORBIDDEN", message, traceId = HttpContext.TraceIdentifier }
    });
}

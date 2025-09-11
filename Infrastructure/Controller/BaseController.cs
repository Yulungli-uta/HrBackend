using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WsUtaSystem.Infrastructure.Controller
{
    //[ApiController]
    //[Route("api/v1/rh/[controller]")]
    //public abstract class BaseController : ControllerBase
    //{
    //    protected readonly ILogger<BaseController> _logger;

    //    protected BaseController(ILogger<BaseController> logger)
    //    {
    //        _logger = logger;
    //    }

    //    /// <summary>
    //    /// Maneja respuestas de error de validación de modelo
    //    /// </summary>
    //    protected IActionResult HandleModelStateErrors()
    //    {
    //        var errors = ModelState.Values
    //            .SelectMany(v => v.Errors)
    //            .Select(e => e.ErrorMessage)
    //            .ToList();

    //        return BadRequest(new
    //        {
    //            Message = "Error de validación",
    //            Errors = errors
    //        });
    //    }

    //    /// <summary>
    //    /// Maneja respuestas de operaciones que devuelven resultados
    //    /// </summary>
    //    protected IActionResult HandleResult<T>(T result, string notFoundMessage = "Registro no encontrado")
    //    {
    //        if (result == null)
    //            return NotFound(new { Message = notFoundMessage });

    //        return Ok(result);
    //    }

    //    /// <summary>
    //    /// Maneja respuestas de operaciones que solo indican éxito
    //    /// </summary>
    //    protected IActionResult HandleSuccess(string message = "Operación completada exitosamente")
    //    {
    //        return Ok(new { Message = message });
    //    }

    //    /// <summary>
    //    /// Maneja excepciones de manera consistente
    //    /// </summary>
    //    protected IActionResult HandleException(Exception ex, string context)
    //    {
    //        _logger.LogError(ex, "Error en {Context}", context);

    //        return StatusCode(500, new
    //        {
    //            Message = "Error interno del servidor",
    //            Detail = "Ocurrió un error inesperado al procesar la solicitud"
    //        });
    //    }
    //}
}

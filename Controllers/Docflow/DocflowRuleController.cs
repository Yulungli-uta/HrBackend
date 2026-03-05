using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Docflow;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.Docflow
{
    /// <summary>
    /// Controlador para gestionar reglas de documentos en Docflow.
    /// Solo orquesta llamadas al servicio, sin lógica de negocio.
    /// 
    /// Rutas generadas:
    /// - GET    /api/v1/docflow/processes/{processId}/rules                (obtener reglas del proceso)
    /// - POST   /api/v1/docflow/processes/{processId}/rules                (crear nueva regla)
    /// - PUT    /api/v1/docflow/processes/{processId}/rules/{ruleId}       (actualizar regla)
    /// - DELETE /api/v1/docflow/processes/{processId}/rules/{ruleId}       (eliminar regla)
    /// </summary>
    [ApiController]
    [Route("processes/{processId:int}/rules")]
    [Produces("application/json")]

    public sealed class DocflowRuleController : ControllerBase
    {
        private readonly IDocflowService _docflowService;

        public DocflowRuleController(IDocflowService docflowService)
        {
            _docflowService = docflowService ?? throw new ArgumentNullException(nameof(docflowService));
        }

        /// <summary>
        /// Obtiene todas las reglas asociadas a un proceso específico.
        /// </summary>
        /// <param name="processId">ID del proceso</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>Lista de reglas del proceso</returns>
        /// <response code="200">Reglas obtenidas exitosamente</response>
        /// <response code="404">Proceso no encontrado</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<DocumentRuleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByProcess([FromRoute] int processId, CancellationToken ct)
        {
            try
            {
                if (processId <= 0)
                    return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

                var rules = await _docflowService.GetRulesByProcessAsync(processId, ct);
                return Ok(rules);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Crea una nueva regla de documento para un proceso.
        /// </summary>
        /// <param name="processId">ID del proceso al que pertenecerá la regla</param>
        /// <param name="req">Datos de la nueva regla</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>La regla creada</returns>
        /// <response code="201">Regla creada exitosamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="404">Proceso no encontrado</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPost]
        [ProducesResponseType(typeof(DocumentRuleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(
            [FromRoute] int processId,
            [FromBody] CreateDocumentRuleRequestDto req,
            CancellationToken ct)
        {
            try
            {
                if (processId <= 0)
                    return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

                if (req == null)
                    return BadRequest(new { message = "Los datos de la regla son requeridos" });

                var rule = await _docflowService.CreateRuleAsync(processId, req, ct);
                return CreatedAtAction(nameof(GetByProcess), new { processId }, rule);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza una regla de documento existente.
        /// </summary>
        /// <param name="processId">ID del proceso (requerido por la ruta)</param>
        /// <param name="ruleId">ID de la regla a actualizar</param>
        /// <param name="req">Datos actualizados de la regla</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>La regla actualizada</returns>
        /// <response code="200">Regla actualizada exitosamente</response>
        /// <response code="404">Regla no encontrada</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPut("{ruleId:int}")]
        [ProducesResponseType(typeof(DocumentRuleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(
            [FromRoute] int processId,
            [FromRoute] int ruleId,
            [FromBody] UpdateDocumentRuleRequestDto req,
            CancellationToken ct)
        {
            try
            {
                if (processId <= 0)
                    return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

                if (ruleId <= 0)
                    return BadRequest(new { message = "El ID de la regla debe ser mayor a 0" });

                if (req == null)
                    return BadRequest(new { message = "Los datos de actualización son requeridos" });

                var rule = await _docflowService.UpdateRuleAsync(ruleId, req, ct);
                return Ok(rule);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Elimina una regla de documento.
        /// </summary>
        /// <param name="processId">ID del proceso (requerido por la ruta)</param>
        /// <param name="ruleId">ID de la regla a eliminar</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>No content si se elimina correctamente</returns>
        /// <response code="204">Regla eliminada exitosamente</response>
        /// <response code="404">Regla no encontrada</response>
        /// <response code="400">No se puede eliminar la regla (validación de negocio)</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpDelete("{ruleId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete(
            [FromRoute] int processId,
            [FromRoute] int ruleId,
            CancellationToken ct)
        {
            try
            {
                if (processId <= 0)
                    return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

                if (ruleId <= 0)
                    return BadRequest(new { message = "El ID de la regla debe ser mayor a 0" });

                await _docflowService.DeleteRuleAsync(ruleId, ct);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }
}
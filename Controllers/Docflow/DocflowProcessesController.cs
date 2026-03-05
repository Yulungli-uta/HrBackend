using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Docflow;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.Docflow
{
    /// <summary>
    /// Controlador para gestionar procesos en Docflow.
    /// Solo orquesta llamadas al servicio, sin lógica de negocio.
    /// 
    /// Rutas generadas:
    /// - GET    /api/v1/docflow/processes                       (obtener todos los procesos)
    /// - GET    /api/v1/docflow/processes/{processId}           (obtener proceso específico)
    /// - GET    /api/v1/docflow/processes/{processId}/rules     (obtener reglas del proceso)
    /// - POST   /api/v1/docflow/processes                       (crear nuevo proceso)
    /// - PUT    /api/v1/docflow/processes/{processId}           (actualizar proceso)
    /// - DELETE /api/v1/docflow/processes/{processId}           (eliminar proceso)
    /// - GET    /api/v1/docflow/processes/{processId}/dynamic-fields     (obtener campos dinámicos)
    /// - PUT    /api/v1/docflow/processes/{processId}/dynamic-fields     (actualizar campos dinámicos)
    /// </summary>
    [ApiController]
    [Route("processes")]
    [Produces("application/json")]

    public sealed class DocflowProcessController : ControllerBase
    {
        private readonly IDocflowService _docflowService;

        public DocflowProcessController(IDocflowService docflowService)
        {
            _docflowService = docflowService ?? throw new ArgumentNullException(nameof(docflowService));
        }

        /// <summary>
        /// Obtiene todos los procesos disponibles en Docflow.
        /// </summary>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>Lista de procesos</returns>
        /// <response code="200">Procesos obtenidos exitosamente</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<ProcessDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            try
            {
                var processes = await _docflowService.GetProcessesAsync(ct);
                return Ok(processes);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un proceso específico por su ID.
        /// </summary>
        /// <param name="processId">ID del proceso</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>Datos del proceso</returns>
        /// <response code="200">Proceso obtenido exitosamente</response>
        /// <response code="404">Proceso no encontrado</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpGet("{processId:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ProcessDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById([FromRoute] int processId, CancellationToken ct)
        {
            try
            {
                if (processId <= 0)
                    return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

                var process = await _docflowService.GetProcessByIdAsync(processId, ct);
                return Ok(process);
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
        /// Obtiene todas las reglas asociadas a un proceso específico.
        /// </summary>
        /// <param name="processId">ID del proceso</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>Lista de reglas del proceso</returns>
        /// <response code="200">Reglas obtenidas exitosamente</response>
        /// <response code="404">Proceso no encontrado</response>
        /// <response code="401">Usuario no autenticado</response>
        //[HttpGet("{processId:int}/rules")]
        //[AllowAnonymous]
        //[ProducesResponseType(typeof(IEnumerable<DocumentRuleDto>), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //public async Task<IActionResult> GetRulesByProcess([FromRoute] int processId, CancellationToken ct)
        //{
        //    try
        //    {
        //        if (processId <= 0)
        //            return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

        //        var rules = await _docflowService.GetRulesByProcessAsync(processId, ct);
        //        return Ok(rules);
        //    }
        //    catch (KeyNotFoundException ex)
        //    {
        //        return NotFound(new { message = ex.Message });
        //    }
        //    catch (UnauthorizedAccessException ex)
        //    {
        //        return Unauthorized(new { message = ex.Message });
        //    }
        //}

        /// <summary>
        /// Crea un nuevo proceso en Docflow.
        /// </summary>
        /// <param name="req">Datos del nuevo proceso</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>El proceso creado</returns>
        /// <response code="201">Proceso creado exitosamente</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPost]
        [ProducesResponseType(typeof(ProcessDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateProcessRequestDto req, CancellationToken ct)
        {
            try
            {
                if (req == null)
                    return BadRequest(new { message = "Los datos del proceso son requeridos" });

                var process = await _docflowService.CreateProcessAsync(req, ct);
                return CreatedAtAction(nameof(GetById), new { processId = process.ProcessId }, process);
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
        /// Actualiza un proceso existente.
        /// </summary>
        /// <param name="processId">ID del proceso a actualizar</param>
        /// <param name="req">Datos actualizados del proceso</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>El proceso actualizado</returns>
        /// <response code="200">Proceso actualizado exitosamente</response>
        /// <response code="404">Proceso no encontrado</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpPut("{processId:int}")]
        [ProducesResponseType(typeof(ProcessDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(
            [FromRoute] int processId,
            [FromBody] UpdateProcessRequestDto req,
            CancellationToken ct)
        {
            try
            {
                if (processId <= 0)
                    return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

                if (req == null)
                    return BadRequest(new { message = "Los datos de actualización son requeridos" });

                var process = await _docflowService.UpdateProcessAsync(processId, req, ct);
                return Ok(process);
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
        /// Elimina un proceso de Docflow.
        /// </summary>
        /// <param name="processId">ID del proceso a eliminar</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>No content si se elimina correctamente</returns>
        /// <response code="204">Proceso eliminado exitosamente</response>
        /// <response code="404">Proceso no encontrado</response>
        /// <response code="400">No se puede eliminar el proceso (validación de negocio)</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpDelete("{processId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Delete([FromRoute] int processId, CancellationToken ct)
        {
            try
            {
                if (processId <= 0)
                    return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

                await _docflowService.DeleteProcessAsync(processId, ct);
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

        /// <summary>
        /// Obtiene los campos dinámicos configurados para un proceso.
        /// </summary>
        /// <param name="processId">ID del proceso</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>Configuración de campos dinámicos</returns>
        /// <response code="200">Campos dinámicos obtenidos exitosamente</response>
        /// <response code="404">Proceso no encontrado</response>
        /// <response code="401">Usuario no autenticado</response>
        [HttpGet("{processId:int}/dynamic-fields")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetDynamicFields([FromRoute] int processId, CancellationToken ct)
        {
            try
            {
                if (processId <= 0)
                    return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

                var dynamicFields = await _docflowService.GetProcessDynamicFieldsAsync(processId, ct);
                return Ok(dynamicFields);
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
        /// Actualiza los campos dinámicos de un proceso.
        /// Requiere permisos de administrador de Docflow.
        /// </summary>
        /// <param name="processId">ID del proceso</param>
        /// <param name="req">Nueva configuración de campos dinámicos</param>
        /// <param name="ct">Token de cancelación</param>
        /// <returns>Confirmación de actualización</returns>
        /// <response code="200">Campos dinámicos actualizados exitosamente</response>
        /// <response code="404">Proceso no encontrado</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="401">Usuario no autenticado</response>
        /// <response code="403">Usuario sin permisos suficientes</response>
        //[Authorize(Roles = "ADMIN,DOCFLOW_ADMIN")]
        [HttpPut("{processId:int}/dynamic-fields")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateDynamicFields(
            [FromRoute] int processId,
            [FromBody] UpdateProcessDynamicFieldsRequest req,
            CancellationToken ct)
        {
            try
            {
                if (processId <= 0)
                    return BadRequest(new { message = "El ID del proceso debe ser mayor a 0" });

                if (req == null)
                    return BadRequest(new { message = "Los datos de campos dinámicos son requeridos" });

                await _docflowService.UpdateProcessDynamicFieldsAsync(processId, req, ct);
                return Ok(new { message = "Campos dinámicos actualizados exitosamente" });
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
    }
}
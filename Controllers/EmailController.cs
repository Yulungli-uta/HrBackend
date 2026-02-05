using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.DTOs.Email;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers
{
    [ApiController]
    [Route("email")]
    public sealed class EmailController : ControllerBase
    {
        private readonly IEmailSenderService _emailSender;

        public EmailController(IEmailSenderService emailSender)
        {
            _emailSender = emailSender;
        }

        /// <summary>
        /// Envío híbrido (multipart/form-data):
        /// - Adjuntos existentes: Attachments[i].StoredFileGuid
        /// - Adjuntos nuevos: Attachments[i].File + metadata (DirectoryCode, EntityType, EntityId, DocumentTypeId, RelativePath)
        /// </summary>
        [HttpPost("send")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Send([FromForm] EmailSendRequestDto request, CancellationToken ct)
        {
            var res = await _emailSender.SendAsync(request, ct);

            if (res.Success)
                return Ok(res);

            return StatusCode(StatusCodes.Status502BadGateway, res);
        }

        /// <summary>
        /// Envío SOLO con GUIDs existentes (application/json).
        /// NO acepta archivos nuevos; solo StoredFileGuid.
        /// </summary>
        [HttpPost("send-by-guid")]
        [Consumes("application/json")]
        public async Task<IActionResult> SendByGuid([FromBody] EmailSendRequestDto request, CancellationToken ct)
        {
            if (request.Attachments is not null && request.Attachments.Any(a => a?.File is not null))
                return BadRequest(new { message = "Este endpoint solo acepta StoredFileGuid; no acepta File." });

            var res = await _emailSender.SendAsync(request, ct);

            if (res.Success)
                return Ok(res);

            return StatusCode(StatusCodes.Status502BadGateway, res);
        }
    }
}

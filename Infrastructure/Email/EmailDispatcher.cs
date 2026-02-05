using WsUtaSystem.Application.DTOs.Email;
using WsUtaSystem.Application.Interfaces.Email;

namespace WsUtaSystem.Infrastructure.Email
{
    /// <summary>
    /// MEJORADO: Pre-procesa IFormFile antes de encolar para evitar stream cerrado.
    /// </summary>
    public class EmailDispatcher : IEmailDispatcher
    {
        private readonly IBackgroundTaskQueue<EmailSendRequestDto> _queue;
        private readonly ILogger<EmailDispatcher> _logger;

        public EmailDispatcher(
            IBackgroundTaskQueue<EmailSendRequestDto> queue,
            ILogger<EmailDispatcher> logger)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask QueueAsync(EmailSendRequestDto request, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            try
            {
                // CRÍTICO: Pre-procesar IFormFile antes de encolar
                var processedRequest = await PreProcessFilesAsync(request, ct);

                await _queue.EnqueueAsync(processedRequest, ct);

                _logger.LogInformation(
                    "[EMAIL-DISPATCH] Enqueued to={To} subject={Subject} attachments={AttCount} q={Count}/{Cap}",
                    processedRequest.To,
                    processedRequest.Subject,
                    processedRequest.Attachments?.Count ?? 0,
                    _queue.Count,
                    _queue.Capacity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[EMAIL-DISPATCH] Failed enqueue to={To} subject={Subject} q={Count}/{Cap}",
                    request.To,
                    request.Subject,
                    _queue.Count,
                    _queue.Capacity);
                throw;
            }
        }

        /// <summary>
        /// Pre-procesa IFormFile convirtiéndolos a bytes en memoria.
        /// Esto evita que el worker intente leer un stream cerrado cuando procese la cola.
        /// </summary>
        private async Task<EmailSendRequestDto> PreProcessFilesAsync(
            EmailSendRequestDto request,
            CancellationToken ct)
        {
            if (request.Attachments == null || request.Attachments.Count == 0)
                return request;

            var processedAttachments = new List<EmailSendAttachmentDto>();

            foreach (var attachment in request.Attachments)
            {
                if (attachment == null)
                    continue;

                // Caso A: StoredFileGuid - no requiere procesamiento
                if (attachment.StoredFileGuid.HasValue && attachment.StoredFileGuid.Value != Guid.Empty)
                {
                    processedAttachments.Add(attachment);
                    continue;
                }

                // Caso B: IFormFile - requiere pre-procesamiento
                if (attachment.File != null && attachment.File.Length > 0)
                {
                    try
                    {
                        // Leer archivo completo en memoria
                        using var memoryStream = new MemoryStream();
                        await attachment.File.CopyToAsync(memoryStream, ct);
                        var fileBytes = memoryStream.ToArray();

                        var processedAttachment = new EmailSendAttachmentDto
                        {
                            // Convertir IFormFile a PreProcessedFile
                            PreProcessedFile = new PreProcessedFileDto
                            {
                                FileBytes = fileBytes,
                                FileName = attachment.File.FileName,
                                ContentType = attachment.File.ContentType ?? "application/octet-stream",
                                FileSize = attachment.File.Length
                            },
                            // Mantener metadata
                            DirectoryCode = attachment.DirectoryCode,
                            EntityType = attachment.EntityType,
                            EntityId = attachment.EntityId,
                            DocumentTypeId = attachment.DocumentTypeId,
                            RelativePath = attachment.RelativePath
                        };

                        processedAttachments.Add(processedAttachment);

                        _logger.LogDebug(
                            "[EMAIL-DISPATCH] Pre-processed file={FileName} size={Size} bytes",
                            attachment.File.FileName,
                            fileBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "[EMAIL-DISPATCH] Failed pre-processing file={FileName}",
                            attachment.File.FileName);

                        // Decisión: ¿continuar sin este adjunto o fallar toda la operación?
                        // Aquí fallamos toda la operación para consistencia
                        throw new InvalidOperationException(
                            $"Failed to pre-process file '{attachment.File.FileName}': {ex.Message}",
                            ex);
                    }
                }
                // Caso C: PreProcessedFile ya presente
                else if (attachment.PreProcessedFile != null)
                {
                    processedAttachments.Add(attachment);
                }
            }

            // Retornar nueva instancia con adjuntos procesados
            return new EmailSendRequestDto
            {
                To = request.To,
                Subject = request.Subject,
                BodyHtml = request.BodyHtml,
                LayoutSlug = request.LayoutSlug,
                CreatedBy = request.CreatedBy,
                Attachments = processedAttachments
            };
        }
    }
}

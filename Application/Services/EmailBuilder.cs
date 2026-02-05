using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Email;
using WsUtaSystem.Application.Interfaces.Email;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services
{
    /// <summary>
    /// MEJORADO: Mejor manejo de errores y validaciones más robustas.
    /// </summary>
    public class EmailBuilder : IEmailBuilder
    {
        private readonly IEmailSenderService _sender;
        private readonly IEmailDispatcher _dispatcher;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<EmailBuilder> _logger;
        private readonly EmailTemplatesOptions _emailTemplates;

        private EmailSendRequestDto _req = new();

        public EmailBuilder(
            IEmailSenderService sender,
            IEmailDispatcher dispatcher,
            ICurrentUserService currentUser,
            IOptions<EmailTemplatesOptions> emailTemplates,
            ILogger<EmailBuilder> logger)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailTemplates = emailTemplates.Value ?? new EmailTemplatesOptions();
        }

        public IEmailBuilder To(string email)
        {
            _req.To = (email ?? string.Empty).Trim();
            return this;
        }

        public IEmailBuilder Subject(string subject)
        {
            _req.Subject = subject ?? string.Empty;
            return this;
        }

        public IEmailBuilder WithBody(string htmlBody)
        {
            _req.BodyHtml = htmlBody ?? string.Empty;
            return this;
        }

        public IEmailBuilder WithLayout(string? slug)
        {
            _req.LayoutSlug = string.IsNullOrWhiteSpace(slug) ? null : slug.Trim();
            return this;
        }

        public IEmailBuilder CreatedBy(int? createdBy)
        {
            _req.CreatedBy = createdBy;
            return this;
        }

        public IEmailBuilder AddStoredAttachment(Guid storedFileGuid)
        {
            if (storedFileGuid == Guid.Empty) return this;

            _req.Attachments ??= new();
            _req.Attachments.Add(new EmailSendAttachmentDto
            {
                StoredFileGuid = storedFileGuid
            });

            return this;
        }

        public IEmailBuilder AddNewAttachment(
            IFormFile file,
            string directoryCode,
            string entityType,
            string entityId,
            int? documentTypeId = null,
            string? relativePath = null)
        {
            if (file == null || file.Length <= 0) return this;

            _req.Attachments ??= new();
            _req.Attachments.Add(new EmailSendAttachmentDto
            {
                File = file,
                DirectoryCode = directoryCode,
                EntityType = entityType,
                EntityId = entityId,
                DocumentTypeId = documentTypeId,
                RelativePath = relativePath
            });

            return this;
        }

        public EmailSendRequestDto Build()
        {
            Validate(_req);

            var snapshot = _req;
            _req = new(); // reset para reuso seguro

            return snapshot;
        }

        public async Task<EmailSendResponseDto> SendAsync(CancellationToken ct = default)
        {
            var req = Build();
            return await _sender.SendAsync(req, ct);
        }

        public async Task QueueAsync(CancellationToken ct = default)
        {
            var req = Build();
            await _dispatcher.QueueAsync(req, ct);
        }

        /// <summary>
        /// MEJORADO: Mejor aislamiento de errores y timeout configurado.
        /// Usa CancellationToken.None para el enqueue interno (evita propagación de cancelación HTTP).
        /// </summary>
        public async Task TryNotifyAsync(
            EmailTemplateKey templateKey,
            string subject,
            string htmlBody,
            string? to = null,
            int timeoutSeconds = 15,
            CancellationToken ct = default)
        {
            try
            {
                var finalTo = (to ?? _currentUser.Email)?.Trim();
                if (string.IsNullOrWhiteSpace(finalTo))
                {
                    _logger.LogWarning(
                        "[EMAIL-BUILDER] TryNotify skipped: recipient null/empty. Template={Template}",
                        templateKey);
                    return;
                }

                if (!_emailTemplates.Layouts.TryGetValue(templateKey.ToString(), out var layoutSlug))
                {
                    _logger.LogWarning(
                        "[EMAIL-BUILDER] TryNotify skipped: layout not found. Template={Template}",
                        templateKey);
                    return;
                }

                // Token con timeout para toda la operación
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
                _logger.LogInformation("********enviando asunto: {subject}, correo: {finalTo}", subject, finalTo);
                await this
                    .To(finalTo)
                    .Subject(subject)
                    .WithLayout(layoutSlug)
                    .WithBody(htmlBody)
                    .CreatedBy(_currentUser.EmployeeId)
                    .QueueAsync(linkedCts.Token);

                _logger.LogDebug(
                    "[EMAIL-BUILDER] TryNotify queued. To={To} Template={Template}",
                    finalTo, templateKey);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timeout propio, no del caller
                _logger.LogWarning(
                    "[EMAIL-BUILDER] TryNotify timeout after {Seconds}s. Template={Template}",
                    timeoutSeconds, templateKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[EMAIL-BUILDER] TryNotify error (best-effort). Template={Template}",
                    templateKey);
            }
        }

        private static void Validate(EmailSendRequestDto req)
        {
            if (string.IsNullOrWhiteSpace(req.To))
                throw new InvalidOperationException("El destinatario (To) es obligatorio.");

            if (string.IsNullOrWhiteSpace(req.Subject))
                throw new InvalidOperationException("El asunto (Subject) es obligatorio.");

            if (string.IsNullOrWhiteSpace(req.BodyHtml))
                throw new InvalidOperationException("El cuerpo (BodyHtml) es obligatorio.");
        }
    }

}

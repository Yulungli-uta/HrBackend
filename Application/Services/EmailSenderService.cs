using Microsoft.Extensions.Options;
using MimeKit;
using System.Diagnostics;
using WsUtaSystem.Application.Common.Email;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Documents;
using WsUtaSystem.Application.DTOs.Email;
using WsUtaSystem.Application.Interfaces.Email;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    /// <summary>
    /// MEJORADO: Tokens de cancelación aislados por operación (SMTP, DB, Upload).
    /// Manejo robusto de errores y logging estructurado.
    /// </summary>
    public class EmailSenderService : IEmailSenderService
    {
        private readonly IEmailProvider _provider;
        private readonly IEmailLayoutsService _layoutsService;
        private readonly IEmailLogsService _logsService;
        private readonly IDocumentOrchestratorService _documents;
        private readonly ICurrentUserService _currentUser;
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailSenderService> _logger;

        // Timeouts configurables
        private static readonly TimeSpan SmtpTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DbLogTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan RenderTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan UploadTimeout = TimeSpan.FromSeconds(60);

        public EmailSenderService(
            IEmailProvider provider,
            IEmailLayoutsService layoutsService,
            IEmailLogsService logsService,
            IDocumentOrchestratorService documents,
            ICurrentUserService currentUser,
            IOptions<EmailSettings> settings,
            ILogger<EmailSenderService> logger)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _layoutsService = layoutsService ?? throw new ArgumentNullException(nameof(layoutsService));
            _logsService = logsService ?? throw new ArgumentNullException(nameof(logsService));
            _documents = documents ?? throw new ArgumentNullException(nameof(documents));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<EmailSendResponseDto> SendAsync(EmailSendRequestDto request, CancellationToken ct)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var swTotal = Stopwatch.StartNew();
            var createdBy = ResolveCreatedBy(request);

            _logger.LogInformation(
                "[EMAIL] START to={To} subject={Subject} layout={Layout} attachments={AttCount} createdBy={CreatedBy}",
                request.To, request.Subject, request.LayoutSlug,
                request.Attachments?.Count ?? 0, createdBy);

            // 1) Render
            string renderedHtml;
            var swRender = Stopwatch.StartNew();
            try
            {
                using var renderCts = new CancellationTokenSource(RenderTimeout);
                using var linkedRenderCts = CancellationTokenSource.CreateLinkedTokenSource(ct, renderCts.Token);

                renderedHtml = await RenderAsync(request, linkedRenderCts.Token);
                swRender.Stop();
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                swRender.Stop();
                _logger.LogWarning("[EMAIL] Render timeout after {Ms}ms", swRender.ElapsedMilliseconds);

                return new EmailSendResponseDto
                {
                    Success = false,
                    Message = "Timeout rendering email layout."
                };
            }

            // 2) Construir mensaje+adjuntos
            MimeMessage message;
            List<EmailLogAttachment> logAttachments;
            var swBuild = Stopwatch.StartNew();
            try
            {
                using var buildCts = new CancellationTokenSource(UploadTimeout);
                using var linkedBuildCts = CancellationTokenSource.CreateLinkedTokenSource(ct, buildCts.Token);

                (message, logAttachments) = await BuildMessageAndLogAttachmentsAsync(
                    request, renderedHtml, createdBy, linkedBuildCts.Token);
                swBuild.Stop();
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                swBuild.Stop();
                _logger.LogWarning("[EMAIL] Build/Upload timeout after {Ms}ms", swBuild.ElapsedMilliseconds);

                return new EmailSendResponseDto
                {
                    Success = false,
                    Message = "Timeout processing attachments."
                };
            }
            catch (Exception ex)
            {
                swBuild.Stop();
                _logger.LogError(ex, "[EMAIL] Build/Upload failed after {Ms}ms", swBuild.ElapsedMilliseconds);

                return new EmailSendResponseDto
                {
                    Success = false,
                    Message = $"Failed processing attachments: {ex.Message}"
                };
            }

            // 3) Log base
            var log = new EmailLog
            {
                Recipient = (request.To ?? string.Empty).Trim(),
                Subject = request.Subject ?? string.Empty,
                BodyRendered = renderedHtml,
                SentAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy,
                Status = "Queued"
            };

            try
            {
                // 4) SMTP con token aislado
                var swSmtp = Stopwatch.StartNew();
                try
                {
                    using var smtpCts = new CancellationTokenSource(SmtpTimeout);
                    using var linkedSmtpCts = CancellationTokenSource.CreateLinkedTokenSource(ct, smtpCts.Token);

                    await _provider.SendAsync(message, linkedSmtpCts.Token);
                    swSmtp.Stop();
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // Timeout propio de SMTP
                    swSmtp.Stop();
                    throw new TimeoutException($"SMTP timeout after {swSmtp.ElapsedMilliseconds}ms");
                }

                log.Status = "Sent";
                log.ErrorMessage = null;

                // 5) DB log con token aislado (CRÍTICO: NO usar token del request HTTP)
                var swDb = Stopwatch.StartNew();
                int logId;
                try
                {
                    using var dbCts = new CancellationTokenSource(DbLogTimeout);
                    // NO vincular con ct para evitar que cancelación HTTP afecte el logging
                    logId = await _logsService.InsertLogWithAttachmentsAsync(log, logAttachments, dbCts.Token);
                    swDb.Stop();
                }
                catch (OperationCanceledException)
                {
                    // Timeout del logging (no crítico, email ya se envió)
                    swDb.Stop();
                    _logger.LogWarning(
                        "[EMAIL] DB log timeout after {Ms}ms (email was sent successfully)",
                        swDb.ElapsedMilliseconds);

                    logId = 0; // No pudimos guardar el log
                }

                swTotal.Stop();

                _logger.LogInformation(
                    "[EMAIL] OK EmailLogID={Id} render={R}ms build={B}ms smtp={S}ms db={D}ms total={T}ms",
                    logId, swRender.ElapsedMilliseconds, swBuild.ElapsedMilliseconds,
                    swSmtp.ElapsedMilliseconds, swDb.ElapsedMilliseconds, swTotal.ElapsedMilliseconds);

                return new EmailSendResponseDto
                {
                    Success = true,
                    EmailLogID = logId > 0 ? logId : null,
                    Message = "Email enviado correctamente."
                };
            }
            catch (Exception ex)
            {
                swTotal.Stop();

                _logger.LogError(ex,
                    "[EMAIL] FAIL to={To} subject={Subject} total={T}ms",
                    request.To, request.Subject, swTotal.ElapsedMilliseconds);

                log.Status = "Failed";
                log.ErrorMessage = ex.Message;

                // Intentar guardar log de error (best-effort con token aislado)
                int? logId = null;
                try
                {
                    using var dbCts = new CancellationTokenSource(DbLogTimeout);
                    logId = await _logsService.InsertLogWithAttachmentsAsync(log, logAttachments, dbCts.Token);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "[EMAIL] FAIL writing error EmailLog");
                }

                return new EmailSendResponseDto
                {
                    Success = false,
                    EmailLogID = logId,
                    Message = $"Error enviando email: {ex.Message}"
                };
            }
        }

        private int? ResolveCreatedBy(EmailSendRequestDto request)
        {
            if (_currentUser.IsAuthenticated && _currentUser.EmployeeId.HasValue)
                return _currentUser.EmployeeId;

            return request.CreatedBy;
        }

        private async Task<string> RenderAsync(EmailSendRequestDto request, CancellationToken ct)
        {
            var body = request.BodyHtml ?? string.Empty;

            if (string.IsNullOrWhiteSpace(request.LayoutSlug))
                return body;

            var slug = request.LayoutSlug.Trim();

            var layout = await _layoutsService.GetBySlugAsync(slug, ct);
            if (layout is null || !layout.IsActive)
            {
                _logger.LogWarning(
                    "[EMAIL] Layout not found/disabled slug={Slug}. Sending only BodyHtml.",
                    slug);
                return body;
            }

            var header = layout.HeaderHtml ?? string.Empty;
            var footer = layout.FooterHtml ?? string.Empty;

            return $"{header}{body}{footer}";
        }

        private async Task<(MimeMessage msg, List<EmailLogAttachment> logAttachments)> BuildMessageAndLogAttachmentsAsync(
            EmailSendRequestDto request,
            string renderedHtml,
            int? createdBy,
            CancellationToken ct)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            msg.To.Add(MailboxAddress.Parse((request.To ?? string.Empty).Trim()));
            msg.Subject = request.Subject ?? string.Empty;

            var builder = new BodyBuilder
            {
                HtmlBody = renderedHtml
            };

            var logAttachments = new List<EmailLogAttachment>();

            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                foreach (var a in request.Attachments)
                {
                    if (a is null) continue;

                    // Caso A: StoredFileGuid existente
                    if (a.StoredFileGuid.HasValue && a.StoredFileGuid.Value != Guid.Empty)
                    {
                        var file = await _documents.DownloadByGuidAsync(a.StoredFileGuid.Value, ct);
                        if (file is null)
                        {
                            _logger.LogWarning(
                                "[EMAIL] Attachment guid not found {Guid}",
                                a.StoredFileGuid);
                            continue;
                        }

                        builder.Attachments.Add(
                            file.Value.fileName,
                            file.Value.fileBytes,
                            ContentType.Parse(file.Value.contentType));

                        logAttachments.Add(new EmailLogAttachment
                        {
                            StoredFileGuid = a.StoredFileGuid.Value,
                            FileName = file.Value.fileName,
                            ContentType = file.Value.contentType,
                            CreatedAt = DateTime.Now,
                            CreatedBy = createdBy
                        });

                        continue;
                    }

                    // Caso B: PreProcessedFile (viene de la cola)
                    if (a.PreProcessedFile != null && a.PreProcessedFile.FileBytes.Length > 0)
                    {
                        if (string.IsNullOrWhiteSpace(a.DirectoryCode) ||
                            string.IsNullOrWhiteSpace(a.EntityType) ||
                            string.IsNullOrWhiteSpace(a.EntityId))
                        {
                            _logger.LogWarning(
                                "[EMAIL] PreProcessed attachment skipped: missing metadata. file={File}",
                                a.PreProcessedFile.FileName);
                            continue;
                        }

                        // Crear MemoryStream desde bytes pre-procesados
                        var memoryStream = new MemoryStream(a.PreProcessedFile.FileBytes);

                        // Crear FormFile simulado para DocumentOrchestrator
                        var formFile = new FormFileFromMemory(
                            memoryStream,
                            a.PreProcessedFile.FileName,
                            a.PreProcessedFile.ContentType,
                            a.PreProcessedFile.FileSize);

                        var uploadReq = new DocumentUploadSingleRequestDto
                        {
                            DirectoryCode = a.DirectoryCode!,
                            EntityType = a.EntityType!,
                            EntityId = a.EntityId!,
                            RelativePath = a.RelativePath,
                            DocumentTypeId = a.DocumentTypeId,
                            File = formFile
                        };

                        var uploadRes = await _documents.UploadSingleAndRegisterAsync(uploadReq, ct);
                        var storedGuid = uploadRes?.Items?.FirstOrDefault()?.StoredFile?.FileGuid;

                        if (uploadRes is null || !uploadRes.Success ||
                            !storedGuid.HasValue || storedGuid.Value == Guid.Empty)
                        {
                            _logger.LogWarning(
                                "[EMAIL] UploadSingleAndRegister failed. file={File} msg={Msg}",
                                a.PreProcessedFile.FileName, uploadRes?.Message);
                            continue;
                        }

                        var storedFile = await _documents.DownloadByGuidAsync(storedGuid.Value, ct);
                        if (storedFile is null)
                        {
                            _logger.LogWarning(
                                "[EMAIL] Download after upload failed. guid={Guid}",
                                storedGuid);
                            continue;
                        }

                        builder.Attachments.Add(
                            storedFile.Value.fileName,
                            storedFile.Value.fileBytes,
                            ContentType.Parse(storedFile.Value.contentType));

                        logAttachments.Add(new EmailLogAttachment
                        {
                            StoredFileGuid = storedGuid.Value,
                            FileName = storedFile.Value.fileName,
                            ContentType = storedFile.Value.contentType,
                            CreatedAt = DateTime.Now,
                            CreatedBy = createdBy
                        });

                        continue;
                    }

                    // Caso C: IFormFile directo (envío sincrónico, no encolado)
                    if (a.File != null && a.File.Length > 0)
                    {
                        if (string.IsNullOrWhiteSpace(a.DirectoryCode) ||
                            string.IsNullOrWhiteSpace(a.EntityType) ||
                            string.IsNullOrWhiteSpace(a.EntityId))
                        {
                            _logger.LogWarning(
                                "[EMAIL] New attachment skipped: missing metadata. file={File}",
                                a.File.FileName);
                            continue;
                        }

                        var uploadReq = new DocumentUploadSingleRequestDto
                        {
                            DirectoryCode = a.DirectoryCode!,
                            EntityType = a.EntityType!,
                            EntityId = a.EntityId!,
                            RelativePath = a.RelativePath,
                            DocumentTypeId = a.DocumentTypeId,
                            File = a.File
                        };

                        var uploadRes = await _documents.UploadSingleAndRegisterAsync(uploadReq, ct);
                        var storedGuid = uploadRes?.Items?.FirstOrDefault()?.StoredFile?.FileGuid;

                        if (uploadRes is null || !uploadRes.Success ||
                            !storedGuid.HasValue || storedGuid.Value == Guid.Empty)
                        {
                            _logger.LogWarning(
                                "[EMAIL] UploadSingleAndRegister failed. file={File} msg={Msg}",
                                a.File.FileName, uploadRes?.Message);
                            continue;
                        }

                        var storedFile = await _documents.DownloadByGuidAsync(storedGuid.Value, ct);
                        if (storedFile is null)
                        {
                            _logger.LogWarning(
                                "[EMAIL] Download after upload failed. guid={Guid}",
                                storedGuid);
                            continue;
                        }

                        builder.Attachments.Add(
                            storedFile.Value.fileName,
                            storedFile.Value.fileBytes,
                            ContentType.Parse(storedFile.Value.contentType));

                        logAttachments.Add(new EmailLogAttachment
                        {
                            StoredFileGuid = storedGuid.Value,
                            FileName = storedFile.Value.fileName,
                            ContentType = storedFile.Value.contentType,
                            CreatedAt = DateTime.Now,
                            CreatedBy = createdBy
                        });
                    }
                }
            }

            msg.Body = builder.ToMessageBody();
            return (msg, logAttachments);
        }

        /// <summary>
        /// Helper: Implementación de IFormFile desde MemoryStream para archivos pre-procesados.
        /// </summary>
        private class FormFileFromMemory : IFormFile
        {
            private readonly MemoryStream _stream;
            public string ContentType { get; }
            public string ContentDisposition => $"attachment; filename=\"{FileName}\"";
            public IHeaderDictionary Headers => new HeaderDictionary();
            public long Length { get; }
            public string Name => FileName;
            public string FileName { get; }

            public FormFileFromMemory(MemoryStream stream, string fileName, string contentType, long length)
            {
                _stream = stream;
                FileName = fileName;
                ContentType = contentType;
                Length = length;
            }

            public void CopyTo(Stream target) => _stream.CopyTo(target);
            public Task CopyToAsync(Stream target, CancellationToken ct = default)
                => _stream.CopyToAsync(target, ct);
            public Stream OpenReadStream() => _stream;
        }
    }
}

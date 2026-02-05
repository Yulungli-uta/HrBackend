using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class EmailLogsService : Service<EmailLog, int>, IEmailLogsService
    {
        private readonly IEmailLogsRepository _logsRepository;
        private readonly IEmailLogAttachmentsRepository _attachmentsRepository;
        private readonly AppDbContext _db;
        private readonly ILogger<EmailLogsService> _logger;

        public EmailLogsService(
            IEmailLogsRepository logsRepo,
            IEmailLogAttachmentsRepository attachmentsRepo,
            AppDbContext dbContext,
            ILogger<EmailLogsService> logger) : base(logsRepo)
        {
            _logsRepository = logsRepo ?? throw new ArgumentNullException(nameof(logsRepo));
            _attachmentsRepository = attachmentsRepo ?? throw new ArgumentNullException(nameof(attachmentsRepo));
            _db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<IEnumerable<EmailLog>> GetByRecipientAsync(string recipient, CancellationToken ct = default)
            => _logsRepository.GetByRecipientAsync(recipient, ct);

        public async Task<int> InsertLogWithAttachmentsAsync(
            EmailLog log,
            IEnumerable<EmailLogAttachment> attachments,
            CancellationToken ct)
        {
            if (log is null) throw new ArgumentNullException(nameof(log));

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);

                _db.EmailLogs.Add(log);
                await _db.SaveChangesAsync(ct);

                var list = (attachments ?? Enumerable.Empty<EmailLogAttachment>())
                    .Where(a => a != null)
                    .ToList();

                if (list.Count > 0)
                {
                    foreach (var a in list)
                        a.EmailLogID = log.EmailLogID;

                    _db.EmailLogAttachments.AddRange(list);
                    await _db.SaveChangesAsync(ct);
                }

                await tx.CommitAsync(ct);

                _logger.LogInformation("[EMAIL-LOGS] Inserted EmailLogID={Id} attachments={Count}",
                    log.EmailLogID, list.Count);

                return log.EmailLogID;
            });
        }
    }
}

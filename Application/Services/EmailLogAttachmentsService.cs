using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class EmailLogAttachmentsService : Service<EmailLogAttachment, int>, IEmailLogAttachmentsService
    {
        private readonly IEmailLogAttachmentsRepository _repository;

        public EmailLogAttachmentsService(IEmailLogAttachmentsRepository repo) : base(repo)
        {
            _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public Task<IEnumerable<EmailLogAttachment>> GetByEmailLogIdAsync(int emailLogId, CancellationToken ct = default)
        {
            if (emailLogId <= 0) return Task.FromResult<IEnumerable<EmailLogAttachment>>(Array.Empty<EmailLogAttachment>());
            return _repository.GetByEmailLogIdAsync(emailLogId, ct);
        }
    }
}

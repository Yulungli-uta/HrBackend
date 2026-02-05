using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IEmailLogAttachmentsService : IService<EmailLogAttachment, int>
    {
        Task<IEnumerable<EmailLogAttachment>> GetByEmailLogIdAsync(int emailLogId, CancellationToken ct);
    }
}

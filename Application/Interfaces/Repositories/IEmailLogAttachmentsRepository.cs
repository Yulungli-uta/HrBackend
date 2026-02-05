using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IEmailLogAttachmentsRepository : IRepository<EmailLogAttachment, int> 
    {
        Task<IEnumerable<EmailLogAttachment>> GetByEmailLogIdAsync(int emailLogId, CancellationToken ct);
    }
}

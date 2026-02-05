using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IEmailLogsRepository : IRepository<EmailLog, int> 
    {
        Task<IEnumerable<EmailLog>> GetByRecipientAsync(string recipient, CancellationToken ct);
    }
}

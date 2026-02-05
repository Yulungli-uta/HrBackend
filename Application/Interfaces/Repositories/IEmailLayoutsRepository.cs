using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IEmailLayoutsRepository : IRepository<EmailLayout, int> 
    {
        Task<EmailLayout?> GetBySlugAsync(string slug, CancellationToken ct);
    }
}

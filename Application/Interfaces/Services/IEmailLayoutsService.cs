using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IEmailLayoutsService : IService<EmailLayout, int>
    {
        Task<EmailLayout?> GetBySlugAsync(string slug, CancellationToken ct);
    }
}

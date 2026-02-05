using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class EmailLayoutsService : Service<EmailLayout, int>, IEmailLayoutsService
    {
        private readonly IEmailLayoutsRepository _repository;

        public EmailLayoutsService(IEmailLayoutsRepository repo) : base(repo)
        {
            _repository = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public Task<EmailLayout?> GetBySlugAsync(string slug, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(slug)) return Task.FromResult<EmailLayout?>(null);
            return _repository.GetBySlugAsync(slug.Trim(), ct);
        }
    }
}

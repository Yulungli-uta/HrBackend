using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class EmailLayoutsRepository : ServiceAwareEfRepository<EmailLayout, int>, IEmailLayoutsRepository
    {
        private readonly AppDbContext _db;
        public EmailLayoutsRepository(AppDbContext dbContext) : base(dbContext) 
        {
            _db = dbContext;
        }

        public Task<EmailLayout?> GetBySlugAsync(string slug, CancellationToken ct)
        {
            slug = (slug ?? string.Empty).Trim();
            if (slug.Length == 0) return Task.FromResult<EmailLayout?>(null);

            return _db.EmailLayouts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == slug, ct);
        }
    }
}

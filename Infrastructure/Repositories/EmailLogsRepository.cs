using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class EmailLogsRepository : ServiceAwareEfRepository<EmailLog, int>, IEmailLogsRepository
    {
        private readonly AppDbContext _db;
        public EmailLogsRepository(AppDbContext dbContext) : base(dbContext) 
        { 
            _db = dbContext;
        }

        public async  Task<IEnumerable<EmailLog>> GetByRecipientAsync(string recipient, CancellationToken ct)
        {
            recipient = (recipient ?? string.Empty).Trim();
            if (recipient.Length == 0) return Enumerable.Empty<EmailLog>();

            return await _db.EmailLogs
                .AsNoTracking()
                .Where(x => x.Recipient == recipient)
                .OrderByDescending(x => x.SentAt)
                .ToListAsync(ct);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class EmailLogAttachmentsRepository : ServiceAwareEfRepository<EmailLogAttachment, int>, IEmailLogAttachmentsRepository
    {
        private readonly AppDbContext _db;
        public EmailLogAttachmentsRepository(AppDbContext dbContext) : base(dbContext) { 
            _db = dbContext;
        }

        public async Task<IEnumerable<EmailLogAttachment>> GetByEmailLogIdAsync(int emailLogId, CancellationToken ct)
        {
            return await _db.EmailLogAttachments
                .AsNoTracking()
                .Where(x => x.EmailLogID == emailLogId)
                .OrderByDescending(x => x.EmailLogAttachmentID)
                .ToListAsync(ct);
        }
    }
}

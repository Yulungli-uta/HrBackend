using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class DirectoryParametersRepository : ServiceAwareEfRepository<DirectoryParameters, int>, IDirectoryParametersRepository
{
    public DirectoryParametersRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<DirectoryParameters?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await _db.Set<DirectoryParameters>()
            .FirstOrDefaultAsync(d => d.Code == code && d.Status, ct);
    }
}


using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Data;
namespace WsUtaSystem.Infrastructure.Common;
public class ServiceAwareEfRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class
{
    protected readonly AppDbContext _db; protected readonly DbSet<TEntity> _set;
    public ServiceAwareEfRepository(AppDbContext db) { _db = db; _set = _db.Set<TEntity>(); }
    public Task<List<TEntity>> GetAllAsync(CancellationToken ct) => _set.AsNoTracking().ToListAsync(ct);
    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct) => _set.FindAsync(new object?[] { id }, ct).AsTask();
    public async Task AddAsync(TEntity entity, CancellationToken ct) { _set.Add(entity); await _db.SaveChangesAsync(ct); }
    public async Task UpdateAsync(TEntity entity, CancellationToken ct) { _db.Entry(entity).State = EntityState.Modified; await _db.SaveChangesAsync(ct); }
    public async Task DeleteAsync(TEntity entity, CancellationToken ct) { _set.Remove(entity); await _db.SaveChangesAsync(ct); }
    public IQueryable<TEntity> Query() => _set.AsQueryable();
}

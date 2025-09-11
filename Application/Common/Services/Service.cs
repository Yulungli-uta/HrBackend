using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Common.Services;
public class Service<TEntity, TKey> : IService<TEntity, TKey> where TEntity : class
{
    protected readonly IRepository<TEntity, TKey> _repo;
    public Service(IRepository<TEntity, TKey> repo) => _repo = repo;
    public Task<List<TEntity>> GetAllAsync(CancellationToken ct) => _repo.GetAllAsync(ct);
    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct) => _repo.GetByIdAsync(id, ct);
    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct) { await _repo.AddAsync(entity, ct); return entity; }
    public async Task UpdateAsync(TKey id, TEntity entity, CancellationToken ct) { await _repo.UpdateAsync(entity, ct); }
    public async Task DeleteAsync(TKey id, CancellationToken ct)
    { var e = await _repo.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Recurso no encontrado."); await _repo.DeleteAsync(e, ct); }
}

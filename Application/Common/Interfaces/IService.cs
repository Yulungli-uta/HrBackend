namespace WsUtaSystem.Application.Common.Interfaces;
public interface IService<TEntity, TKey> where TEntity : class
{
    Task<List<TEntity>> GetAllAsync(CancellationToken ct);
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct);
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct);
    Task UpdateAsync(TKey id, TEntity entity, CancellationToken ct);
    Task DeleteAsync(TKey id, CancellationToken ct);
}

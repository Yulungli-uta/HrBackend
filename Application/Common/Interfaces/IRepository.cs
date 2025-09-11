using System.Linq.Expressions;
namespace WsUtaSystem.Application.Common.Interfaces;
public interface IRepository<TEntity, TKey> where TEntity : class
{
    Task<List<TEntity>> GetAllAsync(CancellationToken ct);
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct);
    Task AddAsync(TEntity entity, CancellationToken ct);
    Task UpdateAsync(TEntity entity, CancellationToken ct);
    Task DeleteAsync(TEntity entity, CancellationToken ct);
    IQueryable<TEntity> Query();
}

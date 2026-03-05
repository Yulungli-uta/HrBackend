using System.Linq.Expressions;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;

namespace WsUtaSystem.Application.Common.Services;

/// <summary>
/// Implementación base genérica del servicio de dominio.
/// Delega las operaciones CRUD al repositorio correspondiente.
/// Los servicios específicos del dominio pueden heredar de esta clase
/// y sobrescribir los métodos que requieran lógica de negocio adicional.
/// </summary>
public class Service<TEntity, TKey> : IService<TEntity, TKey> where TEntity : class
{
    protected readonly IRepository<TEntity, TKey> _repo;

    public Service(IRepository<TEntity, TKey> repo) =>
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    /// <inheritdoc/>
    [Obsolete("Puede causar problemas de rendimiento en tablas grandes. " +
              "Use GetPagedAsync para resultados paginados.")]
    public Task<List<TEntity>> GetAllAsync(CancellationToken ct) => _repo.GetAllAsync(ct);

    /// <inheritdoc/>
    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct) =>
        _repo.GetByIdAsync(id, ct);

    /// <inheritdoc/>
    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct)
    {
        await _repo.AddAsync(entity, ct);
        return entity;
    }

    /// <inheritdoc/>
    public Task UpdateAsync(TKey id, TEntity entity, CancellationToken ct) =>
        _repo.UpdateAsync(id, entity, ct);

    /// <inheritdoc/>
    public Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes) =>
        _repo.GetPagedAsync(page, pageSize, ct, orderBy, ascending, includes);

    /// <inheritdoc/>
    public Task<PagedResult<TEntity>> GetPagedAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int page,
        int pageSize,
        CancellationToken ct,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes) =>
        _repo.GetPagedAsync(predicate, page, pageSize, ct, orderBy, ascending, includes);

    /// <inheritdoc/>
    public async Task DeleteAsync(TKey id, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException(
                $"{typeof(TEntity).Name} con clave [{id}] no encontrado para eliminación.");
        await _repo.DeleteAsync(entity, ct);
    }
}

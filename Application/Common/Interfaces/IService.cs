using System.Linq.Expressions;
using WsUtaSystem.Application.DTOs.Common;

namespace WsUtaSystem.Application.Common.Interfaces;

/// <summary>
/// Contrato genérico de servicio de dominio para operaciones CRUD y consultas avanzadas.
/// </summary>
/// <typeparam name="TEntity">Tipo de entidad del dominio.</typeparam>
/// <typeparam name="TKey">Tipo de la clave primaria.</typeparam>
public interface IService<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Retorna todos los registros sin paginación.
    /// ADVERTENCIA: Usar únicamente en catálogos pequeños.
    /// Para conjuntos grandes, utilizar <see cref="GetPagedAsync"/>.
    /// </summary>
    [Obsolete("Puede causar problemas de rendimiento en tablas grandes. " +
              "Use GetPagedAsync para resultados paginados.")]
    Task<List<TEntity>> GetAllAsync(CancellationToken ct);

    /// <summary>Obtiene una entidad por su clave primaria.</summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct);

    /// <summary>Crea y persiste una nueva entidad.</summary>
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct);

    /// <summary>Actualiza una entidad existente identificada por su clave primaria.</summary>
    Task UpdateAsync(TKey id, TEntity entity, CancellationToken ct);

    /// <summary>Elimina una entidad identificada por su clave primaria.</summary>
    Task DeleteAsync(TKey id, CancellationToken ct);

    /// <summary>
    /// Retorna un resultado paginado con soporte de ordenamiento y eager loading.
    /// Método preferido para todos los listados que puedan crecer en volumen.
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// Retorna un resultado paginado con filtro dinámico para búsqueda en servidor.
    /// Usar cuando se requiere filtrar por texto u otros criterios específicos de la entidad.
    /// </summary>
    /// <param name="predicate">Expresión de filtro (WHERE). Si es null, no aplica filtro.</param>
    Task<PagedResult<TEntity>> GetPagedAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int page,
        int pageSize,
        CancellationToken ct,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes);
}

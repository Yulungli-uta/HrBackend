using System.Linq.Expressions;
using WsUtaSystem.Application.DTOs.Common;

namespace WsUtaSystem.Application.Common.Interfaces;

/// <summary>
/// Contrato genérico de repositorio para operaciones CRUD y consultas avanzadas.
/// Aplica el principio de Segregación de Interfaces (ISP): los consumidores
/// solo dependen de los métodos que realmente necesitan.
/// </summary>
/// <typeparam name="TEntity">Tipo de entidad del dominio.</typeparam>
/// <typeparam name="TKey">Tipo de la clave primaria.</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : class
{
    // ─────────────────────────────────────────────────────────────────────────
    // Consultas básicas
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retorna todos los registros sin paginación.
    /// ADVERTENCIA: Usar únicamente en catálogos pequeños o con filtros previos.
    /// Para conjuntos grandes, utilizar <see cref="GetPagedAsync"/>.
    /// </summary>
    [Obsolete("Puede causar problemas de rendimiento en tablas grandes. " +
              "Use GetPagedAsync para resultados paginados o " +
              "GetAllAsync(includes) si necesita relaciones específicas.")]
    Task<List<TEntity>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Retorna todos los registros incluyendo las relaciones especificadas.
    /// ADVERTENCIA: Usar únicamente en catálogos pequeños.
    /// Para conjuntos grandes, utilizar <see cref="GetPagedAsync"/>.
    /// </summary>
    /// <param name="includes">Expresiones de navegación a incluir (Eager Loading).</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<List<TEntity>> GetAllAsync(
        CancellationToken ct,
        params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// Retorna un resultado paginado con soporte de ordenamiento y eager loading.
    /// Método preferido para todos los listados que puedan crecer en volumen.
    /// </summary>
    /// <param name="page">Número de página (base 1).</param>
    /// <param name="pageSize">Cantidad de registros por página (máximo 100).</param>
    /// <param name="orderBy">Expresión de ordenamiento. Si es null, ordena por clave primaria.</param>
    /// <param name="ascending">true = ascendente, false = descendente.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <param name="includes">Expresiones de navegación a incluir (Eager Loading).</param>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes);

    /// <summary>Obtiene una entidad por su clave primaria.</summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct);

    // ─────────────────────────────────────────────────────────────────────────
    // Comandos (escritura)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Inserta una nueva entidad en la base de datos.</summary>
    Task AddAsync(TEntity entity, CancellationToken ct);

    /// <summary>
    /// Actualiza una entidad existente identificada por su clave primaria.
    /// La entidad debe haber sido obtenida previamente mediante <see cref="GetByIdAsync"/>
    /// para garantizar el rastreo correcto por EF Core.
    /// </summary>
    Task UpdateAsync(TKey id, TEntity entity, CancellationToken ct);

    /// <summary>Elimina una entidad de la base de datos.</summary>
    Task DeleteAsync(TEntity entity, CancellationToken ct);

    // ─────────────────────────────────────────────────────────────────────────
    // Acceso directo al queryable (para consultas avanzadas en repositorios específicos)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Expone el <see cref="IQueryable{T}"/> subyacente para consultas avanzadas.
    /// Usar con precaución: siempre aplicar filtros antes de materializar.
    /// </summary>
    IQueryable<TEntity> Query();
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Data;

namespace WsUtaSystem.Infrastructure.Common;

/// <summary>
/// Implementación genérica del repositorio basada en Entity Framework Core.
/// Provee operaciones CRUD estándar y consultas avanzadas con paginación y eager loading.
/// Aplica el patrón Repository para desacoplar la capa de datos de la lógica de negocio.
/// </summary>
/// <typeparam name="TEntity">Tipo de entidad del dominio.</typeparam>
/// <typeparam name="TKey">Tipo de la clave primaria.</typeparam>
public class ServiceAwareEfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<TEntity> _set;

    public ServiceAwareEfRepository(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _set = _db.Set<TEntity>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Consultas básicas
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    [Obsolete("Puede causar problemas de rendimiento en tablas grandes. " +
              "Use GetPagedAsync para resultados paginados o " +
              "GetAllAsync(ct, includes) si necesita relaciones específicas.")]
    public Task<List<TEntity>> GetAllAsync(CancellationToken ct) =>
        _set.AsNoTracking().ToListAsync(ct);

    /// <inheritdoc/>
    public Task<List<TEntity>> GetAllAsync(
        CancellationToken ct,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _set.AsNoTracking();

        foreach (var include in includes)
            query = query.Include(include);

        return query.ToListAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes)
    {
        // Validar y normalizar parámetros de paginación
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<TEntity> query = _set.AsNoTracking();

        // Aplicar eager loading
        foreach (var include in includes)
            query = query.Include(include);

        // Contar total ANTES de paginar para calcular TotalPages correctamente
        var totalCount = await query.LongCountAsync(ct);

        if (totalCount == 0)
            return PagedResult<TEntity>.Empty(page, pageSize);

        // Aplicar ordenamiento
        query = orderBy is not null
            ? (ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy))
            : query; // Sin ordenamiento explícito: EF usará el orden natural de la BD

        // Aplicar paginación
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TEntity>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public async Task<PagedResult<TEntity>> GetPagedAsync(
        Expression<Func<TEntity, bool>>? predicate,
        int page,
        int pageSize,
        CancellationToken ct,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true,
        params Expression<Func<TEntity, object>>[] includes)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<TEntity> query = _set.AsNoTracking();

        // Aplicar filtro dinámico (búsqueda en servidor)
        if (predicate is not null)
            query = query.Where(predicate);

        // Aplicar eager loading
        foreach (var include in includes)
            query = query.Include(include);

        // Contar total después del filtro para paginación correcta
        var totalCount = await query.LongCountAsync(ct);
        if (totalCount == 0)
            return PagedResult<TEntity>.Empty(page, pageSize);

        // Aplicar ordenamiento
        query = orderBy is not null
            ? (ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy))
            : query;

        // Aplicar paginación
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TEntity>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct) =>
        _set.FindAsync(new object?[] { id }, ct).AsTask();

    // ─────────────────────────────────────────────────────────────────────────
    // Comandos (escritura)
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task AddAsync(TEntity entity, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _set.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Estrategia de actualización segura:
    /// 1. Carga la entidad existente por PK (garantiza rastreo por EF Core).
    /// 2. Copia los valores del objeto recibido sobre la entidad rastreada.
    /// 3. EF Core genera un UPDATE solo con las columnas modificadas.
    /// Esto evita sobreescribir campos de auditoría (CreatedAt, CreatedBy)
    /// y previene conflictos de concurrencia.
    /// </remarks>
    public async Task UpdateAsync(TKey id, TEntity entity, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // 1. Buscar la entidad existente por clave primaria
        var existing = await _set.FindAsync(new object?[] { id }, ct)
            ?? throw new KeyNotFoundException(
                $"{typeof(TEntity).Name} con clave [{id}] no encontrado para actualización.");

        // 2. Obtener metadatos de la entidad para proteger la PK
        var entityType = _db.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"No se pudo resolver el EntityType para {typeof(TEntity).Name}.");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException(
                $"La entidad {typeof(TEntity).Name} no tiene clave primaria definida.");

        // 3. Asegurar que la entidad recibida tenga las mismas claves que la existente
        //    para que EF Core NO intente modificar la PK
        foreach (var keyProp in primaryKey.Properties)
        {
            var propInfo = keyProp.PropertyInfo
                ?? throw new InvalidOperationException(
                    $"No se pudo obtener PropertyInfo para la propiedad '{keyProp.Name}' en {typeof(TEntity).Name}.");

            propInfo.SetValue(entity, propInfo.GetValue(existing));
        }

        // 4. Copiar valores del objeto recibido sobre la entidad ya rastreada
        //    EF Core detectará solo los campos que cambiaron
        _db.Entry(existing).CurrentValues.SetValues(entity);

        // 5. Persistir únicamente los cambios detectados
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(TEntity entity, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _set.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Acceso directo al queryable
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public IQueryable<TEntity> Query() => _set.AsQueryable();
}

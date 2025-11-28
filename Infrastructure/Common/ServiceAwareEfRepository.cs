using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Data;

namespace WsUtaSystem.Infrastructure.Common
{
    public class ServiceAwareEfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
        where TEntity : class
    {
        protected readonly AppDbContext _db;
        protected readonly DbSet<TEntity> _set;

        public ServiceAwareEfRepository(AppDbContext db)
        {
            _db = db;
            _set = _db.Set<TEntity>();
        }

        public Task<List<TEntity>> GetAllAsync(CancellationToken ct) =>
            _set.AsNoTracking().ToListAsync(ct);

        public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct) =>
            _set.FindAsync(new object?[] { id }, ct).AsTask();

        public async Task AddAsync(TEntity entity, CancellationToken ct)
        {
            _set.Add(entity);
            await _db.SaveChangesAsync(ct);
        }

        // ✅ UPDATE usando el ID y sin intentar modificar la clave primaria
        public async Task UpdateAsync(TKey id, TEntity entity, CancellationToken ct)
        {
            // 1. Buscar la entidad existente por clave primaria
            var existing = await _set.FindAsync(new object?[] { id }, ct);

            if (existing is null)
            {
                throw new KeyNotFoundException(
                    $"{typeof(TEntity).Name} con clave [{id}] no encontrado para actualización.");
            }

            // 2. Obtener metadatos de la entidad
            var entityType = _db.Model.FindEntityType(typeof(TEntity))
                ?? throw new InvalidOperationException(
                    $"No se pudo resolver el EntityType para {typeof(TEntity).Name}");

            var key = entityType.FindPrimaryKey()
                ?? throw new InvalidOperationException(
                    $"La entidad {typeof(TEntity).Name} no tiene clave primaria definida.");

            // 3. Asegurar que la entidad "nueva" tenga las mismas claves que la existente
            //    para que EF NO intente cambiar la PK
            foreach (var keyProp in key.Properties)
            {
                var propInfo = keyProp.PropertyInfo
                    ?? throw new InvalidOperationException(
                        $"No se pudo obtener PropertyInfo para la propiedad de clave {keyProp.Name} en {typeof(TEntity).Name}");

                var currentKeyValue = propInfo.GetValue(existing);
                propInfo.SetValue(entity, currentKeyValue);
            }

            // 4. Copiar valores del objeto enviado sobre la entidad ya trackeada
            var entry = _db.Entry(existing);
            entry.CurrentValues.SetValues(entity);

            // 5. Guardar cambios
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync1(TEntity entity, CancellationToken ct) { _db.Entry(entity).State = EntityState.Modified; await _db.SaveChangesAsync(ct); }

        public async Task DeleteAsync(TEntity entity, CancellationToken ct)
        {
            _set.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }

        public IQueryable<TEntity> Query() => _set.AsQueryable();
    }
}

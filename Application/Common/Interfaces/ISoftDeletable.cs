namespace WsUtaSystem.Application.Common.Interfaces;

/// <summary>
/// Marca una entidad como soft-delete: "eliminar" pone <see cref="IsDeleted"/> en true
/// en vez de borrar la fila físicamente. <c>ServiceAwareEfRepository&lt;TEntity,TKey&gt;.DeleteAsync</c>
/// detecta esta interfaz; <c>AppDbContext.OnModelCreating</c> aplica un filtro global de consulta
/// para excluir automáticamente las filas marcadas de toda consulta EF Core normal. No aplica a
/// SQL/Dapper crudo (reportes, consultas especializadas) — esas deben filtrar IsDeleted manualmente.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}

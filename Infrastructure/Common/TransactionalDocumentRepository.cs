using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Common;

/// <summary>
/// Coordina, en UNA sola transacción SQL, la creación de una entidad de dominio junto con
/// su fila de metadata en HR.tbl_StoredFile (cuando aplica). El archivo físico (NAS) NUNCA
/// puede ser parte de la misma transacción SQL — se sube antes de llamar aquí, y si esta
/// transacción falla, el llamador debe revertir (borrar) el archivo físico ya subido.
/// </summary>
public class TransactionalDocumentRepository
{
    private readonly AppDbContext _db;

    public TransactionalDocumentRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Inserta <paramref name="entity"/> y, si <paramref name="buildStoredFile"/> no es nulo,
    /// construye y guarda también la fila de StoredFile (usando el Id ya generado de
    /// <paramref name="entity"/>) — ambos inserts ocurren en la misma transacción: si el
    /// segundo falla, el primero también se revierte.
    /// </summary>
    public async Task<(TEntity entity, StoredFile? storedFile)> CreateWithDocumentAsync<TEntity>(
        TEntity entity,
        Func<TEntity, StoredFile>? buildStoredFile,
        CancellationToken ct)
        where TEntity : class
    {
        // El proyecto configura una estrategia de ejecución con reintentos (SqlServerRetryingExecutionStrategy),
        // que no admite transacciones iniciadas manualmente salvo que se ejecuten a través de ella.
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                _db.Set<TEntity>().Add(entity);
                await _db.SaveChangesAsync(ct);

                StoredFile? storedFile = null;
                if (buildStoredFile != null)
                {
                    storedFile = buildStoredFile(entity);
                    _db.Set<StoredFile>().Add(storedFile);
                    await _db.SaveChangesAsync(ct);
                }

                await transaction.CommitAsync(ct);
                return (entity, storedFile);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
}

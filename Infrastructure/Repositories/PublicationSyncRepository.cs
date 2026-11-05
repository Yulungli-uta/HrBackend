using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Data;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class PublicationSyncRepository : IPublicationSyncRepository
{
    private readonly AppDbContext _db;

    public PublicationSyncRepository(AppDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

    public Task<List<Publications>> GetPersonPublicationsAsync(int personId, bool tracked, CancellationToken ct)
    {
        var query = _db.Publications.Where(p => p.PersonId == personId);
        return (tracked ? query : query.AsNoTracking()).ToListAsync(ct);
    }

    /// <remarks>
    /// El contexto usa reintentos automáticos de EF (EnableRetryOnFailure), que no admiten una transacción abierta a mano:
    /// toda la unidad (candado, lectura, escritura y confirmación) debe ejecutarse dentro de la estrategia de ejecución, que
    /// la repite completa ante un fallo transitorio. Por eso cada intento descarta las altas pendientes del anterior.
    /// </remarks>
    public async Task<T?> RunExclusiveAsync<T>(string resource, Func<Task<T>> work, CancellationToken ct) where T : class
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            foreach (var entry in _db.ChangeTracker.Entries<Publications>().Where(e => e.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var res = new SqlParameter("@res", SqlDbType.NVarChar, 255) { Value = resource };
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC @result = sp_getapplock @Resource = @res, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 0",
                new object[] { result, res }, ct);

            // >= 0: candado concedido (0 inmediato, 1 tras esperar); < 0: otra sincronización lo tiene.
            if (result.Value is not int code || code < 0) return null;

            var value = await work();
            await tx.CommitAsync(ct);
            return value;
        });
    }

    public void Add(Publications entity) => _db.Publications.Add(entity);

    public void ClearTracking() => _db.ChangeTracker.Clear();

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public async Task<List<MassSyncTarget>> GetMassTargetsAsync(int providerTypeId, bool requiresProviderId, CancellationToken ct)
    {
        // Personas con empleo activo y ORCID; los IDs del proveedor se usan si existen y son obligatorios si el proveedor los exige.
        var profiles = await (
            from profile in _db.ResearcherProfiles.AsNoTracking()
            where profile.Orcid != null
                  && _db.Employees.Any(e => e.PersonID == profile.PersonId && e.IsActive && !e.IsDeleted)
            select new { profile.ResearcherProfileId, profile.PersonId, profile.Orcid })
            .ToListAsync(ct);

        var idRows = await _db.ResearcherProviderIds.AsNoTracking()
            .Where(i => i.IsActive && i.ProviderTypeId == providerTypeId)
            .Select(i => new { i.ResearcherProfileId, i.ExternalId })
            .ToListAsync(ct);
        var idsByProfile = idRows.ToLookup(i => i.ResearcherProfileId, i => i.ExternalId);

        return profiles
            .Select(p => new MassSyncTarget(p.PersonId, p.Orcid!, idsByProfile[p.ResearcherProfileId].Distinct().ToList()))
            .Where(t => !requiresProviderId || t.ProviderIds.Count > 0)
            .OrderBy(t => t.PersonId)
            .ToList();
    }
}

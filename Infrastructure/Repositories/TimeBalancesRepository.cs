using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories
{
    /// <summary>
    /// HR.tbl_TimeBalances tiene clave compuesta real (EmployeeID, LaborRegimeId) desde el
    /// multi-régimen (2026-07-06), pero este repositorio sigue expuesto como
    /// IRepository&lt;TimeBalances, int&gt; (TKey=EmployeeID) por compatibilidad con los
    /// consumidores existentes (TimeBalancesController, EmployeeSelfServiceService). El
    /// GetByIdAsync/UpdateAsync genéricos de la clase base usan EF Find() con un solo valor,
    /// que revienta contra una clave compuesta — se sobrescriben aquí para resolver por
    /// EmployeeID tomando el primer régimen (determinístico por LaborRegimeId ascendente,
    /// no por significado del Id). Para un empleado con un solo régimen activo (la inmensa
    /// mayoría) esto es exactamente equivalente al comportamiento anterior a la migración.
    /// Multi-régimen real: usar VacationBalanceAdjustmentService (ya resuelve por régimen).
    /// </summary>
    public class TimeBalancesRepository : ServiceAwareEfRepository<TimeBalances, int>, ITimeBalancesRepository
    {
        public TimeBalancesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

        public override Task<TimeBalances?> GetByIdAsync(int id, CancellationToken ct) =>
            _set.Where(t => t.EmployeeID == id)
                .OrderBy(t => t.LaborRegimeId)
                .FirstOrDefaultAsync(ct);

        public override async Task UpdateAsync(int id, TimeBalances entity, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var existing = await GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"TimeBalances con EmployeeID=[{id}] no encontrado para actualización.");

            entity.LaborRegimeId = existing.LaborRegimeId; // preservar la clave compuesta real
            _db.Entry(existing).CurrentValues.SetValues(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}

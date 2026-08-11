using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IVacationsService : IService<Vacations, int> {

    Task<Vacations> CreateWithBalanceCheckAsync(Vacations entity, CancellationToken ct);
    Task<Vacations> UpdateBalanceAffectAsync(int id, Vacations entity, CancellationToken ct);
    Task<IEnumerable<Vacations>> GetByEmployeeId(int EmployeeId, CancellationToken ct);
    Task<IEnumerable<Vacations>> GetByImmediateBossId(int immediateBossId, CancellationToken ct);

    /// <summary>Libera la reserva de saldo activa (si hay) antes de eliminar la fila — a
    /// diferencia del DeleteAsync genérico, que no toca el saldo.</summary>
    Task DeleteWithBalanceReleaseAsync(int id, CancellationToken ct);
}

using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IAcademicLadderRepository : IRepository<AcademicLadder, int>
{
    /// <summary>Devuelve todos los escalafones ordenados por Sequence.</summary>
    Task<List<AcademicLadder>> GetAllOrderedAsync(CancellationToken ct);

    /// <summary>Devuelve el escalafón inmediatamente siguiente al dado.</summary>
    Task<AcademicLadder?> GetNextAsync(int ladderId, CancellationToken ct);
}

using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IPersonnelActionTypeRepository : IRepository<PersonnelActionType, int>
{
    /// <summary>Obtiene todos los tipos activos ordenados por nombre.</summary>
    Task<List<PersonnelActionType>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Consume el siguiente número de secuencia para el año dado.
    /// Usa una transacción serializable para evitar duplicados concurrentes.
    /// </summary>
    Task<(string DocumentNumber, int Year, int Sequence)> ConsumeNextNumberAsync(
        int personnelActionTypeId,
        int year,
        CancellationToken ct = default);
}

using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.PersonnelActionType;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IPersonnelActionTypeService : IService<PersonnelActionType, int>
{
    /// <summary>Obtiene todos los tipos activos.</summary>
    Task<List<PersonnelActionType>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Genera y reserva el siguiente número de documento para el tipo dado.
    /// El número tiene el formato {prefix}-{year}-{seq:D3} (ej: DAP-2026-001).
    /// </summary>
    Task<NextDocumentNumberDto> GetNextNumberAsync(int personnelActionTypeId, CancellationToken ct = default);
}

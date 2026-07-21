using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface ITramiteRequirementsRepository : IRepository<TramiteRequirement, int>
{
    Task<List<TramiteRequirement>> GetByModuleAsync(int moduleTypeId, CancellationToken ct);

    /// <summary>DocumentTypeId de los requisitos obligatorios activos para el módulo y, opcionalmente,
    /// el tipo específico (unión de nivel general + override puntual).</summary>
    Task<List<int>> GetRequiredDocumentTypeIdsAsync(int moduleTypeId, int? specificTypeId, CancellationToken ct);
}

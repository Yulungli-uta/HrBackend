using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IOccupationalGroupRepository : IRepository<OccupationalGroup, int>
{
    /// <summary>Indica si el tipo existe en ref_Types con la categoría de familias de grupo ocupacional.</summary>
    Task<bool> FamilyTypeExistsAsync(int familyTypeId, CancellationToken ct);

    /// <summary>Indica si otro grupo de la misma familia ya usa ese grado (se excluye el grupo que se edita).</summary>
    Task<bool> FamilyDegreeInUseAsync(int familyTypeId, int degreeId, int? excludeGroupId, CancellationToken ct);
}

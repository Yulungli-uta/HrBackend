using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class OccupationalGroupService : Service<OccupationalGroup, int>, IOccupationalGroupService
{
    private readonly IOccupationalGroupRepository _groups;

    public OccupationalGroupService(IOccupationalGroupRepository repo) : base(repo) => _groups = repo;

    /// <inheritdoc/>
    public override async Task<OccupationalGroup> CreateAsync(OccupationalGroup entity, CancellationToken ct)
    {
        await ValidateFamilyAsync(entity, null, ct);
        return await base.CreateAsync(entity, ct);
    }

    /// <inheritdoc/>
    public override async Task UpdateAsync(int id, OccupationalGroup entity, CancellationToken ct)
    {
        await ValidateFamilyAsync(entity, id, ct);
        await base.UpdateAsync(id, entity, ct);
    }

    /// <summary>
    /// La familia, si viene, debe ser un tipo vigente de la categoría correcta y no repetir el grado dentro de ella.
    /// Es un mensaje claro antes de llegar al índice único de la base.
    /// </summary>
    private async Task ValidateFamilyAsync(OccupationalGroup entity, int? groupId, CancellationToken ct)
    {
        if (entity.FamilyTypeId is not int familyId) return;

        if (!await _groups.FamilyTypeExistsAsync(familyId, ct))
            throw new BusinessRuleException("La familia seleccionada no existe o está inactiva.");

        if (await _groups.FamilyDegreeInUseAsync(familyId, entity.DegreeId, groupId, ct))
            throw new BusinessRuleException("Ya existe otro grupo de esa familia con el mismo grado. Cada grado se usa una sola vez por familia.");
    }
}

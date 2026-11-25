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
        await ValidateAndNameAsync(entity, null, null, ct);
        return await base.CreateAsync(entity, ct);
    }

    /// <inheritdoc/>
    public override async Task UpdateAsync(int id, OccupationalGroup entity, CancellationToken ct)
    {
        var current = await base.GetByIdAsync(id, ct);
        await ValidateAndNameAsync(entity, id, current, ct);
        await base.UpdateAsync(id, entity, ct);
    }

    /// <summary>
    /// Reglas de la escala (Grupo x Grado + Nivel): el nivel exige grupo; el grupo debe existir y estar vigente; el grado y el nivel
    /// no se repiten dentro del grupo; el RMU referencial, si viene, no es negativo. Con grupo, el nombre lo compone el servidor como
    /// "Grupo [Prefijo]Nivel" (p. ej. "Servidor Público 5", "Autoridades Académicas Nivel 4") para que no quede texto libre.
    /// Mensajes claros antes de llegar a los índices únicos de la base.
    /// </summary>
    private async Task ValidateAndNameAsync(OccupationalGroup entity, int? groupId, OccupationalGroup? current, CancellationToken ct)
    {
        // Un grado inactivo no se puede asignar (si la escala ya lo tenía, se respeta para poder seguir editándola)
        if (current?.DegreeId != entity.DegreeId && !await _groups.IsDegreeActiveAsync(entity.DegreeId, ct))
            throw new BusinessRuleException("El grado seleccionado está inactivo o no existe. Elija un grado vigente.");

        if (entity.Rmu is < 0 or > 99_999_999.99m)
            throw new BusinessRuleException("El RMU referencial debe estar entre 0 y 99.999.999,99.");
        if (entity.Level is < 1 or > 99)
            throw new BusinessRuleException("El nivel debe estar entre 1 y 99.");

        if (entity.FamilyTypeId is not int familyId)
        {
            if (entity.Level.HasValue)
                throw new BusinessRuleException("Para indicar un nivel hay que elegir primero el grupo.");
            if (string.IsNullOrWhiteSpace(entity.Description))
                throw new BusinessRuleException("Indique el grupo o, mientras se clasifica, el nombre de la escala.");
            return;
        }

        var naming = await _groups.GetFamilyNamingAsync(familyId, ct);
        if (naming is null || !await _groups.FamilyTypeExistsAsync(familyId, ct))
            throw new BusinessRuleException("El grupo seleccionado no existe o está inactivo.");

        if (await _groups.FamilyDegreeInUseAsync(familyId, entity.DegreeId, groupId, ct))
            throw new BusinessRuleException("Ya existe otra escala de ese grupo con el mismo grado. Cada grado se usa una sola vez por grupo.");

        if (entity.Level is int level && await _groups.FamilyLevelInUseAsync(familyId, level, groupId, ct))
            throw new BusinessRuleException("Ya existe otra escala de ese grupo con el mismo nivel. Cada nivel se usa una sola vez por grupo.");

        entity.Description = entity.Level is int n ? $"{naming.Value.Name} {naming.Value.LevelPrefix}{n}" : naming.Value.Name;
    }
}

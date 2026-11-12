using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.PersonnelActionType;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class PersonnelActionTypeService
    : Service<PersonnelActionType, int>, IPersonnelActionTypeService
{
    /// <summary>Tokens admitidos en <see cref="PersonnelActionType.NumberingFormat"/>.</summary>
    private static readonly string[] FormatTokens = ["{PREFIX}", "{SEQ}", "{YEAR}"];

    private const int NumberingFormatMaxLength = 60;

    private readonly IPersonnelActionTypeRepository _repo;

    private readonly IAppClock _clock;

    public PersonnelActionTypeService(IPersonnelActionTypeRepository repo, IAppClock clock) : base(repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _clock = clock;
    }

    public Task<List<PersonnelActionType>> GetAllActiveAsync(CancellationToken ct = default)
        => _repo.GetAllActiveAsync(ct);

    public Task<Application.DTOs.Common.PagedResult<PersonnelActionTypeFamily>> GetFamiliesPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;
        return _repo.GetFamiliesPagedAsync(search?.Trim(), page, pageSize, ct);
    }

    public Task SetDefaultTemplateAsync(int personnelActionTypeId, int? templateId, CancellationToken ct = default)
        => _repo.SetDefaultTemplateAsync(personnelActionTypeId, templateId, ct);

    public async Task<NextDocumentNumberDto> GetNextNumberAsync(
        int personnelActionTypeId,
        CancellationToken ct = default)
    {
        var year = _clock.Now.Year;
        var (docNumber, y, seq) = await _repo.ConsumeNextNumberAsync(personnelActionTypeId, year, ct);

        // El prefijo ya no se puede deducir del número (formatos como DTH-UTA-LPE-2057-2026): se lee del tipo.
        var type = await _repo.GetByIdAsync(personnelActionTypeId, ct);
        return new NextDocumentNumberDto(docNumber, type?.NumberingPrefix ?? docNumber.Split('-')[0], y, seq);
    }

    /// <inheritdoc />
    public async Task<PersonnelActionType> CreateTypeAsync(PersonnelActionType entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ValidateNumberingFormat(entity.NumberingFormat);
        entity.NumberingFormat = NormalizeFormat(entity.NumberingFormat);

        // La secuencia de un tipo nuevo siempre arranca en 0 del año en curso (la semilla de las series con
        // numeración acumulada se fija con el script de sincronización, no desde aquí).
        entity.NumberingYear = _clock.Now.Year;
        entity.NumberingLastSequence = 0;

        if (entity.ParentTypeId is > 0)
        {
            var parent = await ValidateParentAsync(null, entity.ParentTypeId.Value, ct);

            // Un hijo hereda del padre lo que no se indique: categoría, plantilla y homologación SIIES.
            // ReachesVigente no viene en el DTO de alta y es un concepto de nómina del grupo: se copia siempre.
            if (string.IsNullOrWhiteSpace(entity.ActionCategory)) entity.ActionCategory = parent.ActionCategory;
            entity.DefaultTemplateId ??= parent.DefaultTemplateId;
            entity.SiiesRelacionIesTypeId ??= parent.SiiesRelacionIesTypeId;
            entity.ReachesVigente = parent.ReachesVigente;
        }
        else
        {
            entity.ParentTypeId = null;
        }

        return await CreateAsync(entity, ct);
    }

    /// <inheritdoc />
    public async Task UpdateTypeAsync(int personnelActionTypeId, PersonnelActionTypeUpdateDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var current = await _repo.GetByIdAsync(personnelActionTypeId, ct)
            ?? throw new KeyNotFoundException($"PersonnelActionType id={personnelActionTypeId} no existe.");

        // null = conservar lo guardado (un cliente anterior que no envía el campo no debe borrarlo);
        // 0 = dejar como tipo raíz.
        var newParentId = dto.ParentTypeId is null ? current.ParentTypeId : (dto.ParentTypeId > 0 ? dto.ParentTypeId : null);
        if (newParentId.HasValue && newParentId != current.ParentTypeId)
            await ValidateParentAsync(personnelActionTypeId, newParentId.Value, ct);

        ValidateNumberingFormat(dto.NumberingFormat);

        await _repo.UpdateSettingsAsync(personnelActionTypeId, entity =>
        {
            entity.Name                      = dto.Name;
            entity.Code                      = dto.Code;
            entity.Description               = dto.Description;
            entity.NumberingPrefix           = dto.NumberingPrefix;
            entity.DefaultTemplateId         = dto.DefaultTemplateId;
            entity.IsActive                  = dto.IsActive;
            entity.RequiresAdUserCreation    = dto.RequiresAdUserCreation;
            entity.RequiresAdUserDisable     = dto.RequiresAdUserDisable;
            entity.RequiresAdGroupAssignment = dto.RequiresAdGroupAssignment;
            // null = conservar lo guardado (un cliente que no envía estos campos no debe borrarlos: de la categoría
            // depende cómo se imprime la acción); cadena vacía / 0 = borrar el valor.
            entity.ActionCategory            = dto.ActionCategory is null
                ? entity.ActionCategory
                : (string.IsNullOrWhiteSpace(dto.ActionCategory) ? null : dto.ActionCategory.Trim());
            entity.SiiesRelacionIesTypeId    = dto.SiiesRelacionIesTypeId is null
                ? entity.SiiesRelacionIesTypeId
                : (dto.SiiesRelacionIesTypeId > 0 ? dto.SiiesRelacionIesTypeId : null);
            entity.ParentTypeId              = newParentId;
            if (dto.NumberingFormat is not null)
                entity.NumberingFormat = NormalizeFormat(dto.NumberingFormat);
            if (dto.NumberingResetsYearly.HasValue)
                entity.NumberingResetsYearly = dto.NumberingResetsYearly.Value;
            // No se tocan NumberingYear/NumberingLastSequence (contador vivo) ni ReachesVigente.
        }, ct);
    }

    /// <summary>
    /// Valida que <paramref name="parentTypeId"/> sea un padre admisible: existe, no es el propio tipo,
    /// no es a su vez un hijo (la jerarquía es de un solo nivel) y, si el tipo ya existe, no tiene hijos propios.
    /// </summary>
    private async Task<PersonnelActionType> ValidateParentAsync(int? typeId, int parentTypeId, CancellationToken ct)
    {
        if (typeId.HasValue && typeId.Value == parentTypeId)
            throw new BusinessRuleException("Un tipo de acción no puede ser su propio padre.");

        var parent = await _repo.GetByIdAsync(parentTypeId, ct)
            ?? throw new BusinessRuleException($"El tipo padre {parentTypeId} no existe.");

        if (parent.ParentTypeId.HasValue)
            throw new BusinessRuleException(
                $"El tipo '{parent.Name}' ya es un hijo: la jerarquía de tipos de acción es de un solo nivel.");

        if (typeId.HasValue && await _repo.HasChildrenAsync(typeId.Value, ct))
            throw new BusinessRuleException(
                "Este tipo tiene tipos hijos y no puede pasar a ser hijo de otro (un solo nivel).");

        return parent;
    }

    /// <summary>El formato debe incluir {SEQ}, usar solo los tokens admitidos y caber en la columna.</summary>
    private static void ValidateNumberingFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return;

        if (format.Length > NumberingFormatMaxLength)
            throw new BusinessRuleException($"El formato de numeración no puede superar {NumberingFormatMaxLength} caracteres.");

        if (!format.Contains("{SEQ}", StringComparison.Ordinal))
            throw new BusinessRuleException("El formato de numeración debe incluir el token {SEQ}.");

        var residue = format;
        foreach (var token in FormatTokens)
            residue = residue.Replace(token, string.Empty, StringComparison.Ordinal);

        if (residue.Contains('{') || residue.Contains('}'))
            throw new BusinessRuleException("El formato de numeración solo admite los tokens {PREFIX}, {SEQ} y {YEAR}.");
    }

    private static string? NormalizeFormat(string? format)
        => string.IsNullOrWhiteSpace(format) ? null : format.Trim();
}

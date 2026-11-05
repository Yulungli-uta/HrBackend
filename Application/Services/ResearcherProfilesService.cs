using System.Text.RegularExpressions;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.ResearcherProfiles;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class ResearcherProfilesService : IResearcherProfilesService
{
    private const int MaxExternalIdLength = 50;
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(200);

    private readonly IResearcherProfilesRepository _repo;
    private readonly IScientificProviderCatalog _catalog;
    private readonly IAppClock _clock;
    private readonly IOrcidDirectory _orcid;

    public ResearcherProfilesService(IResearcherProfilesRepository repo, IScientificProviderCatalog catalog, IAppClock clock, IOrcidDirectory orcid)
    {
        _orcid = orcid ?? throw new ArgumentNullException(nameof(orcid));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<ResearcherProfileDto> GetAsync(int personId, bool canEdit, CancellationToken ct)
    {
        var profile = await _repo.GetByPersonIdAsync(personId, ct);
        return await ToDtoAsync(personId, profile, canEdit, ct);
    }

    public Task<ResearcherProfile?> GetEntityAsync(int personId, CancellationToken ct) =>
        _repo.GetByPersonIdAsync(personId, ct);

    public async Task<SetOrcidResultDto> SetOrcidAsync(int personId, string orcid, bool acknowledgeNameMismatch, int? actorEmployeeId, CancellationToken ct)
    {
        var canonical = OrcidValidator.Normalize(orcid)
            ?? throw new BusinessRuleException("El ORCID no tiene un formato válido. Use el formato 0000-0000-0000-000X.");
        if (!OrcidValidator.IsValid(canonical))
            throw new BusinessRuleException("El ORCID no es válido: el dígito verificador no coincide. Revise que lo copió completo.");

        if (await _repo.OrcidExistsForOtherPersonAsync(canonical, personId, ct))
            throw new BusinessRuleException("Este ORCID ya está registrado por otra persona.");

        var profile = await _repo.GetByPersonIdAsync(personId, ct);

        // Mismo ORCID que ya tenía: no hay nada que comprobar ni cambiar.
        if (profile is not null && string.Equals(profile.Orcid, canonical, StringComparison.Ordinal))
            return new SetOrcidResultDto(true, false, null, null, await ToDtoAsync(personId, profile, canEdit: true, ct));

        // El servidor decide: no se confía en que la pantalla haya comprobado el ORCID.
        var lookup = await _orcid.LookupAsync(canonical, ct);
        string? orcidName = null;
        string? warning = null;
        switch (lookup.Status)
        {
            case OrcidLookupStatus.NotFound:
                throw new BusinessRuleException("Este ORCID no existe en orcid.org. Revise que lo copió completo.");

            case OrcidLookupStatus.Found:
                orcidName = string.Join(' ', new[] { lookup.GivenNames, lookup.FamilyName }.Where(v => !string.IsNullOrWhiteSpace(v)));
                var person = await _repo.GetPersonNameAsync(personId, ct);
                var match = person is null
                    ? NameMatch.Unknown
                    : PersonNameMatcher.Compare(lookup.GivenNames, lookup.FamilyName, person.Value.FirstName, person.Value.LastName);
                if (match == NameMatch.Mismatch && !acknowledgeNameMismatch)
                    return new SetOrcidResultDto(false, true, orcidName, null, null);
                if (match == NameMatch.Unknown)
                    warning = "Este ORCID no publica su nombre: no se pudo comprobar que corresponda a la persona.";
                break;

            default:
                warning = "No se pudo consultar orcid.org en este momento: el ORCID se guardó sin comprobar que exista ni que corresponda a la persona.";
                break;
        }

        if (profile is null)
        {
            profile = new ResearcherProfile
            {
                PersonId = personId,
                Orcid = canonical,
                OrcidEnteredAt = _clock.Now,
                OrcidEnteredBy = actorEmployeeId,
            };
            await _repo.AddProfileAsync(profile, ct);
        }
        else
        {
            profile.Orcid = canonical;
            profile.OrcidEnteredAt = _clock.Now;
            profile.OrcidEnteredBy = actorEmployeeId;
            await _repo.SaveChangesAsync(ct);
        }

        return new SetOrcidResultDto(true, false, string.IsNullOrEmpty(orcidName) ? null : orcidName, warning,
            await ToDtoAsync(personId, profile, canEdit: true, ct));
    }

    public async Task<ResearcherProviderIdDto> AddProviderIdAsync(int personId, string providerCode, string externalId, CancellationToken ct)
    {
        var profile = await _repo.GetByPersonIdAsync(personId, ct)
            ?? throw new BusinessRuleException("Primero debe registrarse el ORCID de la persona.");

        var provider = await _catalog.GetRequiredAsync(providerCode, ct);
        if (!provider.IsActive)
            throw new BusinessRuleException($"El proveedor {provider.DisplayName} no está activo.");
        if (provider.IsSystem)
            throw new BusinessRuleException($"El origen {provider.DisplayName} no admite IDs de investigador.");
        if (provider.UsesProfileOrcid)
            throw new BusinessRuleException($"{provider.DisplayName} usa el ORCID del perfil: no requiere un ID adicional.");

        var id = (externalId ?? string.Empty).Trim();
        if (id.Length == 0 || id.Length > MaxExternalIdLength)
            throw new BusinessRuleException($"El {provider.IdLabel ?? "ID"} es obligatorio y admite hasta {MaxExternalIdLength} caracteres.");
        if (!string.IsNullOrWhiteSpace(provider.IdPattern) && !MatchesPattern(provider.IdPattern, id))
            throw new BusinessRuleException($"El {provider.IdLabel ?? "ID"} no tiene un formato válido para {provider.DisplayName}.");

        var existing = profile.ProviderIds.FirstOrDefault(x =>
            x.ProviderTypeId == provider.TypeId && string.Equals(x.ExternalId, id, StringComparison.OrdinalIgnoreCase));

        if (existing is { IsActive: true })
            throw new BusinessRuleException("Este ID ya está registrado en su perfil.");

        if (!provider.AllowsMultipleIds && profile.ProviderIds.Any(x => x.ProviderTypeId == provider.TypeId && x.IsActive))
            throw new BusinessRuleException($"Solo se admite un {provider.IdLabel ?? "ID"} de {provider.DisplayName}. Elimine el actual para registrar otro.");

        if (await _repo.ActiveProviderIdOwnedByOtherAsync(provider.TypeId, id, profile.ResearcherProfileId, ct))
            throw new BusinessRuleException("Este ID ya está asignado a otra persona.");

        if (existing is not null)
        {
            // Se reactiva el ID dado de baja antes (el índice único impide insertarlo de nuevo).
            existing.IsActive = true;
            await _repo.SaveChangesAsync(ct);
            return ToDto(existing, provider.Code);
        }

        var created = new ResearcherProviderId
        {
            ResearcherProfileId = profile.ResearcherProfileId,
            ProviderTypeId = provider.TypeId,
            ExternalId = id,
            IsActive = true,
        };
        await _repo.AddProviderIdAsync(created, ct);
        return ToDto(created, provider.Code);
    }

    public async Task RemoveProviderIdAsync(int personId, int providerIdId, CancellationToken ct)
    {
        var profile = await _repo.GetByPersonIdAsync(personId, ct)
            ?? throw new KeyNotFoundException("La persona no tiene perfil de investigador.");
        var item = profile.ProviderIds.FirstOrDefault(x => x.ResearcherProviderIdId == providerIdId && x.IsActive)
            ?? throw new KeyNotFoundException("El ID no existe en el perfil de la persona.");

        item.IsActive = false;
        await _repo.SaveChangesAsync(ct);
    }

    private static bool MatchesPattern(string pattern, string value)
    {
        try
        {
            return Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant, PatternTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            // Patrón mal configurado en el catálogo: no se bloquea al usuario por un error de configuración.
            return true;
        }
    }

    private async Task<ResearcherProfileDto> ToDtoAsync(int personId, ResearcherProfile? profile, bool canEdit, CancellationToken ct)
    {
        if (profile is null) return new ResearcherProfileDto(personId, null, null, Array.Empty<ResearcherProviderIdDto>(), canEdit);

        var providers = await _catalog.GetAllAsync(ct);
        var ids = profile.ProviderIds
            .Where(x => x.IsActive)
            .OrderBy(x => x.ProviderTypeId).ThenBy(x => x.ExternalId)
            .Select(x => ToDto(x, providers.FirstOrDefault(p => p.TypeId == x.ProviderTypeId)?.Code ?? string.Empty))
            .ToList();
        return new ResearcherProfileDto(personId, profile.Orcid, profile.OrcidEnteredAt, ids, canEdit);
    }

    private static ResearcherProviderIdDto ToDto(ResearcherProviderId x, string providerCode) =>
        new(x.ResearcherProviderIdId, x.ProviderTypeId, providerCode, x.ExternalId, x.VerifiedAt, x.LastSyncedAt, x.LastSyncStatus, x.LastSyncSummary);
}

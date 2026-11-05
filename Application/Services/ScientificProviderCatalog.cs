using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.ScientificProviders;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Application.Interfaces.Services.PublicationSources;

namespace WsUtaSystem.Application.Services;

public sealed class ScientificProviderCatalog : IScientificProviderCatalog
{
    private const string CacheKey = "ScientificProviderCatalog";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IRefTypesRepository _refTypes;
    private readonly IMemoryCache _cache;
    private readonly IReadOnlySet<string> _adapterCodes;
    private readonly ILogger<ScientificProviderCatalog> _logger;

    public ScientificProviderCatalog(
        IRefTypesRepository refTypes,
        IMemoryCache cache,
        IEnumerable<IPublicationSourceProvider> adapters,
        ILogger<ScientificProviderCatalog> logger)
    {
        _refTypes = refTypes ?? throw new ArgumentNullException(nameof(refTypes));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _adapterCodes = (adapters ?? throw new ArgumentNullException(nameof(adapters)))
            .Select(a => a.ProviderCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<ScientificProviderInfo>> GetAllAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<ScientificProviderInfo>? cached) && cached is not null)
            return cached;

        var rows = await _refTypes.GetByCategoryAsync(ScientificProviderCodes.Category, ct);
        var list = rows
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
            .Select(Map)
            .ToList();

        _cache.Set(CacheKey, (IReadOnlyList<ScientificProviderInfo>)list, CacheDuration);
        return list;
    }

    public async Task<ScientificProviderInfo?> GetByCodeAsync(string code, CancellationToken ct) =>
        (await GetAllAsync(ct)).FirstOrDefault(p => p.Code.Equals(code?.Trim(), StringComparison.OrdinalIgnoreCase));

    public async Task<ScientificProviderInfo?> GetByTypeIdAsync(int typeId, CancellationToken ct) =>
        (await GetAllAsync(ct)).FirstOrDefault(p => p.TypeId == typeId);

    public async Task<ScientificProviderInfo> GetRequiredAsync(string code, CancellationToken ct) =>
        await GetByCodeAsync(code, ct)
        ?? throw new BusinessRuleException($"El proveedor de publicaciones '{code}' no está configurado.");

    private ScientificProviderInfo Map(Models.RefTypes r)
    {
        string? label = null, pattern = null, profileUrl = null;
        bool requiresId = false, allowsMultiple = false, usesOrcid = false, requiresApproval = true, system = false;

        if (!string.IsNullOrWhiteSpace(r.Metadata))
        {
            try
            {
                using var doc = JsonDocument.Parse(r.Metadata);
                var root = doc.RootElement;
                label = Str(root, "label");
                pattern = Str(root, "idPattern");
                profileUrl = Str(root, "profileUrl");
                requiresId = Flag(root, "requiresProviderId", false);
                allowsMultiple = Flag(root, "allowsMultipleIds", false);
                usesOrcid = Flag(root, "usesProfileOrcid", false);
                requiresApproval = Flag(root, "requiresApproval", true);
                system = Flag(root, "system", false);
            }
            catch (JsonException ex)
            {
                // Metadata mal formada: el proveedor queda con la configuración más restrictiva (requiere aprobación).
                _logger.LogWarning(ex, "Metadata inválida en el proveedor {Provider}; se usan los valores por defecto.", r.Name);
            }
        }

        return new ScientificProviderInfo(
            r.TypeId, r.Name, r.Description ?? r.Name, r.IsActive, r.SortOrder,
            label, pattern, profileUrl, requiresId, allowsMultiple, usesOrcid, requiresApproval, system,
            HasAdapter: _adapterCodes.Contains(r.Name));
    }

    private static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static bool Flag(JsonElement e, string name, bool fallback) =>
        e.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : fallback;
}

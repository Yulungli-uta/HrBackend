using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Infrastructure.Services;

/// <summary>
/// Resuelve permisos de acción efectivos consultando
/// <c>GET {AuthService:Url}/api/role-permissions/effective?roles=...</c> en RepositoryUta
/// (endpoint público por diseño: es metadata de esquema RBAC, no datos de usuario — mismo
/// criterio que el JWKS). Cachea por combinación de roles para no llamar en cada request.
/// </summary>
public class UserActionPermissionService : IUserActionPermissionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserActionPermissionService> _logger;

    private readonly string _authServiceUrl;
    private readonly int _cacheDurationMinutes;

    public UserActionPermissionService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<UserActionPermissionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;

        _authServiceUrl = configuration["AuthService:Url"] ?? "http://localhost:5010";
        _cacheDurationMinutes = int.TryParse(configuration["Authorization:PermissionCacheDurationMinutes"], out var d) ? d : 5;
    }

    private const string SuperuserBypassCode = "ADMIN.ACCESS";

    public async Task<bool> HasPermissionAsync(IEnumerable<string> roles, string permissionCode, CancellationToken ct = default)
    {
        var roleList = roles?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToArray()
            ?? Array.Empty<string>();

        if (roleList.Length == 0)
            return false;

        var effective = await GetEffectivePermissionsAsync(roleList, ct);

        // ADMIN.ACCESS es un bypass universal: quien lo tenga satisface cualquier permiso.
        // Sin esto, un rol "Administrador" con RolePermission incompleto quedaría bloqueado
        // igual que cualquier otro rol una vez que ShadowMode se desactive.
        if (effective.Contains(SuperuserBypassCode, StringComparer.OrdinalIgnoreCase))
            return true;

        return effective.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> GetEffectivePermissionsAsync(string[] roles, CancellationToken ct)
    {
        var cacheKey = $"effective_permissions_{string.Join('|', roles)}";
        if (_cache.TryGetValue<HashSet<string>>(cacheKey, out var cached) && cached is not null)
            return cached;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var query = string.Join('&', roles.Select(r => $"roles={Uri.EscapeDataString(r)}"));
            var url = $"{_authServiceUrl.TrimEnd('/')}/api/role-permissions/effective?{query}";

            var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("No se pudo obtener permisos efectivos ({StatusCode}) para roles {Roles}", response.StatusCode, string.Join(',', roles));
                return new HashSet<string>();
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var parsed = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var result = new HashSet<string>(parsed?.Data ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(_cacheDurationMinutes)));
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo permisos efectivos para roles {Roles}", string.Join(',', roles));
            return new HashSet<string>();
        }
    }

    private class ApiResponse
    {
        public bool Success { get; set; }
        public List<string>? Data { get; set; }
    }
}

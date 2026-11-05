using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.ExternalServices.Common;

/// <summary>
/// Resuelve la URL de un servicio externo desde un parámetro de HR.tbl_Parameters (caché de 5 min) y la valida
/// con <see cref="InstitutionalUrlPolicy"/>. Devuelve null si falta, está inactiva o no pasa la política.
/// </summary>
public sealed class ExternalServiceUrlResolver
{
    private const int CacheMinutes = 5;

    private readonly IParametersService _parameters;
    private readonly IMemoryCache _cache;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ExternalServiceUrlResolver> _logger;

    public ExternalServiceUrlResolver(
        IParametersService parameters, IMemoryCache cache, IHostEnvironment environment,
        ILogger<ExternalServiceUrlResolver> logger)
    {
        _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Lee y valida la URL del parámetro <paramref name="parameterName"/>. Con <paramref name="allowLoopbackInDevelopment"/>
    /// se acepta localhost, pero solo cuando el entorno es Development.
    /// </summary>
    public async Task<Uri?> ResolveAsync(string parameterName, CancellationToken ct, bool allowLoopbackInDevelopment = false)
    {
        var allowLoopback = allowLoopbackInDevelopment && _environment.IsDevelopment();
        var cacheKey = $"ExternalServiceUrl_{parameterName}";
        if (_cache.TryGetValue(cacheKey, out Uri? cached) && cached is not null) return cached;

        var value = (await _parameters.GetByNameAsync(parameterName, ct))
            .Select(p => p.Pvalues?.Trim())
            .FirstOrDefault(v => !string.IsNullOrEmpty(v));
        if (value is null)
        {
            _logger.LogWarning("Parámetro {Name} no configurado o inactivo en HR.tbl_Parameters.", parameterName);
            return null;
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !InstitutionalUrlPolicy.IsAllowed(uri, allowLoopback))
        {
            _logger.LogWarning("El parámetro {Name} no es una URL HTTPS válida de un host institucional.", parameterName);
            return null;
        }

        _cache.Set(cacheKey, uri, TimeSpan.FromMinutes(CacheMinutes));
        return uri;
    }
}

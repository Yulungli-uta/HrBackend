using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace WsUtaSystem.Infrastructure.Services;

/// <summary>
/// Valida tokens JWT (RS256) localmente usando la clave pública publicada por
/// RepositoryUta en /.well-known/jwks.json, sin depender de una llamada remota por request.
/// </summary>
public class LocalJwtValidationService : ITokenValidationService
{
    private const string JwksCacheKey = "local_jwt_jwks";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LocalJwtValidationService> _logger;

    private readonly string _jwksUrl;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _jwksCacheHours;
    private readonly bool _enableLogging;

    public LocalJwtValidationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<LocalJwtValidationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;

        var authServiceUrl = configuration["AuthService:Url"] ?? "http://localhost:5010";
        var jwksPath = configuration["AuthService:JwksUrl"] ?? "/.well-known/jwks.json";
        _jwksUrl = $"{authServiceUrl.TrimEnd('/')}/{jwksPath.TrimStart('/')}";
        _issuer = configuration["AuthService:Issuer"] ?? "WsSeguUta.AuthSystem.API";
        _audience = configuration["AuthService:Audience"] ?? "WsSeguUta.AuthSystem.API";
        _jwksCacheHours = int.TryParse(configuration["AuthService:JwksCacheHours"], out var h) ? h : 24;
        _enableLogging = bool.TryParse(configuration["AuthService:EnableLogging"], out var logging) ? logging : true;
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new TokenValidationResult { IsValid = false, Message = "Token no proporcionado" };
        }

        try
        {
            var signingKeys = await GetSigningKeysAsync();

            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var principal = handler.ValidateToken(token, validationParameters, out _);

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            if (_enableLogging)
                _logger.LogDebug("Token validado localmente (RS256) para {Email}", email);

            return new TokenValidationResult
            {
                IsValid = true,
                UserId = userId,
                Email = email,
                Roles = roles,
                Message = "Token is valid"
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult { IsValid = false, Message = "Token expired" };
        }
        catch (SecurityTokenException ex)
        {
            if (_enableLogging)
                _logger.LogWarning(ex, "Validación local de token JWT fallida");

            return new TokenValidationResult { IsValid = false, Message = "Token is invalid" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado validando token JWT localmente");
            return new TokenValidationResult { IsValid = false, Message = "Error inesperado al validar token" };
        }
    }

    private async Task<IEnumerable<SecurityKey>> GetSigningKeysAsync()
    {
        if (_cache.TryGetValue<IEnumerable<SecurityKey>>(JwksCacheKey, out var cached) && cached is not null)
            return cached;

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = await client.GetAsync(_jwksUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var jwks = new JsonWebKeySet(json);
        var keys = jwks.GetSigningKeys().ToList();

        if (keys.Count == 0)
            throw new InvalidOperationException($"JWKS en {_jwksUrl} no contiene claves de firma utilizables");

        _cache.Set(JwksCacheKey, (IEnumerable<SecurityKey>)keys, TimeSpan.FromHours(_jwksCacheHours));

        if (_enableLogging)
            _logger.LogInformation("Claves JWKS obtenidas y cacheadas por {Hours}h desde {Url}", _jwksCacheHours, _jwksUrl);

        return keys;
    }
}

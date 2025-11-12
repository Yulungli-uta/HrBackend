using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace WsUtaSystem.Infrastructure.Services;

/// <summary>
/// Resultado de la validación de un token JWT
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public string? Message { get; set; }
}

/// <summary>
/// Respuesta del servicio de autenticación al validar un token
/// </summary>
public class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public List<string>? Roles { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Interfaz para el servicio de validación de tokens
/// </summary>
public interface ITokenValidationService
{
    Task<TokenValidationResult> ValidateTokenAsync(string token);
}

/// <summary>
/// Servicio que valida tokens JWT contra el servicio de autenticación centralizado
/// </summary>
public class TokenValidationService : ITokenValidationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TokenValidationService> _logger;
    
    private readonly string _authServiceUrl;
    private readonly string? _clientId;
    private readonly bool _enableCaching;
    private readonly int _cacheDurationMinutes;
    private readonly bool _enableLogging;

    public TokenValidationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<TokenValidationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
        
        // Leer configuración
        _authServiceUrl = _configuration["AuthService:Url"] ?? "http://localhost:5010";
        _clientId = _configuration["AuthService:ClientId"];
        _enableCaching = bool.TryParse(_configuration["AuthService:EnableCaching"], out var caching) && caching;
        _cacheDurationMinutes = int.TryParse(_configuration["AuthService:CacheDurationMinutes"], out var duration) ? duration : 2;
        _enableLogging = bool.TryParse(_configuration["AuthService:EnableLogging"], out var logging) ? logging : true;
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        //Console.WriteLine($"**********valor del token : {token}");
        if (string.IsNullOrWhiteSpace(token))
        {
            if (_enableLogging)
                _logger.LogWarning("Token validation failed: Token is null or empty");
            
            return new TokenValidationResult
            {
                IsValid = false,
                Message = "Token no proporcionado"
            };
        }
        //Console.WriteLine("**********paso la validacion del token nulo o vacio");
        // Verificar cache si está habilitado
        if (_enableCaching)
        {
            var cacheKey = $"token_validation_{token}";
            if (_cache.TryGetValue<TokenValidationResult>(cacheKey, out var cachedResult))
            {
                if (_enableLogging)
                    _logger.LogDebug("Token validation result retrieved from cache for user {Email}", cachedResult?.Email);
                
                return cachedResult!;
            }
        }

        try
        {
            if (_enableLogging)
                _logger.LogInformation("Validating token against auth service at {Url}", _authServiceUrl);

            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var request = new
            {
                token = token,
                clientId = _clientId
            };

            var response = await client.PostAsJsonAsync(
                $"{_authServiceUrl}/api/auth/validate-token",
                request
            );

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                if (_enableLogging)
                    _logger.LogDebug("Auth service response: {Content}", content);

                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    //Console.WriteLine($"**********paso la validacion del apiResponse {apiResponse} ");
                    var result = new TokenValidationResult
                    {
                        IsValid = apiResponse.Data.IsValid,
                        UserId = apiResponse.Data.UserId?.ToString(),
                        Email = apiResponse.Data.Email,
                        Roles = apiResponse.Data.Roles ?? new List<string>(),
                        Message = apiResponse.Data.Message
                    };

                    Console.WriteLine("******** apiResponse ********");
                    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

                    // Guardar en cache si está habilitado y el token es válido
                    if (_enableCaching && result.IsValid)
                    {
                        var cacheKey = $"token_validation_{token}";
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_cacheDurationMinutes));
                        
                        _cache.Set(cacheKey, result, cacheOptions);
                        
                        if (_enableLogging)
                            _logger.LogDebug("Token validation result cached for {Minutes} minutes", _cacheDurationMinutes);
                    }

                    if (_enableLogging)
                    {
                        if (result.IsValid)
                            _logger.LogInformation("Token validated successfully for user {Email}", result.Email);
                        else
                            _logger.LogWarning("Token validation failed: {Message}", result.Message);
                    }

                    return result;
                }
            }

            if (_enableLogging)
                _logger.LogError("Auth service returned non-success status: {StatusCode}", response.StatusCode);

            return new TokenValidationResult
            {
                IsValid = false,
                Message = $"Token validation failed: {response.StatusCode}"
            };
        }
        catch (HttpRequestException ex)
        {
            if (_enableLogging)
                _logger.LogError(ex, "HTTP error while validating token against auth service");
            
            return new TokenValidationResult
            {
                IsValid = false,
                Message = "Error de conexión con el servicio de autenticación"
            };
        }
        catch (TaskCanceledException ex)
        {
            if (_enableLogging)
                _logger.LogError(ex, "Timeout while validating token against auth service");
            
            return new TokenValidationResult
            {
                IsValid = false,
                Message = "Timeout al validar token"
            };
        }
        catch (Exception ex)
        {
            if (_enableLogging)
                _logger.LogError(ex, "Unexpected error while validating token");
            
            return new TokenValidationResult
            {
                IsValid = false,
                Message = "Error inesperado al validar token"
            };
        }
    }

    // Clases auxiliares para deserialización
    private class ApiResponse
    {
        public bool Success { get; set; }
        public ValidateTokenResponse? Data { get; set; }
        public string? Message { get; set; }
    }
}

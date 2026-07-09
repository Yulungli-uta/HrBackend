using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Services;

/// <summary>
/// Implementación única del logo institucional: lee wwwroot/images/institutional/logo-uta.png
/// (ruta configurable vía InstitutionalConfig:LogoPath) y cachea el resultado en memoria, ya que
/// el archivo no cambia en tiempo de ejecución. Registrado como singleton en DI.
/// </summary>
public sealed class InstitutionalLogoService : IInstitutionalLogoService
{
    private readonly IConfiguration _config;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
    private readonly ILogger<InstitutionalLogoService> _logger;

    private readonly Lazy<string?> _filePath;
    private readonly Lazy<string> _dataUri;

    public InstitutionalLogoService(
        IConfiguration config,
        Microsoft.AspNetCore.Hosting.IWebHostEnvironment env,
        ILogger<InstitutionalLogoService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _env    = env    ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _filePath = new Lazy<string?>(ResolveFilePath);
        _dataUri  = new Lazy<string>(BuildDataUri);
    }

    public string? GetLogoFilePath() => _filePath.Value;

    public string GetLogoDataUri() => _dataUri.Value;

    private string? ResolveFilePath()
    {
        var relativePath = _config["InstitutionalConfig:LogoPath"] ?? "images/institutional/logo-uta.png";
        var fullPath = Path.Combine(_env.WebRootPath ?? string.Empty, relativePath);

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("InstitutionalLogoService: logo institucional no encontrado en {Path}.", fullPath);
            return null;
        }

        return fullPath;
    }

    private string BuildDataUri()
    {
        var path = GetLogoFilePath();
        if (path is null)
            return string.Empty;

        var bytes = File.ReadAllBytes(path);
        var mime = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg"            => "image/svg+xml",
            _                 => "image/png"
        };

        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }
}

using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Security;

namespace WsUtaSystem.Controllers.HR;

/// <summary>Identidad visual: logos del login (claro/oscuro) y de reportes. Solo lectura de la lista y escritura con permiso de catálogos.</summary>
[ApiController]
[Route("branding")]
public sealed class BrandingController : ControllerBase
{
    private readonly IBrandingService _branding;
    private readonly ICurrentUserService _currentUser;

    public BrandingController(IBrandingService branding, ICurrentUserService currentUser)
    {
        _branding = branding;
        _currentUser = currentUser;
    }

    /// <summary>Lista los logos configurables y si tienen una imagen propia.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _branding.ListAsync(ct));

    /// <summary>Imagen propia del logo (para la vista previa en la pantalla de administración). 404 si usa el oficial.</summary>
    [HttpGet("{key}/content")]
    public async Task<IActionResult> Content([FromRoute] string key, CancellationToken ct)
    {
        var asset = await _branding.GetContentAsync(key, ct);
        if (asset is null) return NotFound(new { message = "Este logo usa la imagen oficial incluida en la aplicación." });
        return BrandingFileResult.From(this, asset);
    }

    /// <summary>Carga o reemplaza un logo. Máximo 1 MB; PNG, JPEG o SVG (reportes: solo PNG o JPEG).</summary>
    [HttpPut("{key}")]
    [RequirePermission("CATALOGS.UPDATE")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromRoute] string key, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest(new { message = "Seleccione un archivo de imagen." });
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var info = await _branding.SaveAsync(key, file.FileName, file.ContentType ?? string.Empty, ms.ToArray(), _currentUser.EmployeeId, ct);
        return Ok(info);
    }

    /// <summary>Quita el logo propio y vuelve al oficial.</summary>
    [HttpDelete("{key}")]
    [RequirePermission("CATALOGS.UPDATE")]
    public async Task<IActionResult> Remove([FromRoute] string key, CancellationToken ct)
    {
        await _branding.DeleteAsync(key, ct);
        return NoContent();
    }
}

/// <summary>Logos del inicio de sesión: públicos (la pantalla de login no tiene sesión). Solo los logos de login, nunca el de reportes.</summary>
[ApiController]
[Route("public/branding")]
public sealed class PublicBrandingController : ControllerBase
{
    private readonly IBrandingService _branding;

    public PublicBrandingController(IBrandingService branding) { _branding = branding; }

    /// <summary>Versión de cada logo del login; null = usar el logo oficial de la aplicación.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await _branding.GetPublicAsync(ct));

    [HttpGet("{key}")]
    public async Task<IActionResult> Logo([FromRoute] string key, CancellationToken ct)
    {
        if (key is not ("LOGIN_LOGO_LIGHT" or "LOGIN_LOGO_DARK")) return NotFound();
        var asset = await _branding.GetContentAsync(key, ct);
        if (asset is null) return NotFound();
        return BrandingFileResult.From(this, asset);
    }
}

internal static class BrandingFileResult
{
    /// <summary>Devuelve la imagen con caché por versión y cabeceras que impiden ejecutar contenido activo en un SVG.</summary>
    public static IActionResult From(ControllerBase c, BrandAssetContent asset)
    {
        c.Response.Headers["Cache-Control"] = "public, max-age=300";
        c.Response.Headers["ETag"] = $"\"{asset.Version}\"";
        c.Response.Headers["X-Content-Type-Options"] = "nosniff";
        c.Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
        return c.File(asset.Content, asset.ContentType);
    }
}

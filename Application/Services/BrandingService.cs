using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Data;
using WsUtaSystem.Infrastructure.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <inheritdoc cref="IBrandingService"/>
public sealed class BrandingService : IBrandingService
{
    private const int MaxBytes = 1024 * 1024; // 1 MB

    // Clave pública -> (parámetro en HR.TBL_PARAMETERS, etiqueta, admite SVG). Los PDF requieren PNG o JPEG.
    private static readonly Dictionary<string, (string Param, string Label, bool AllowSvg)> Keys = new(StringComparer.Ordinal)
    {
        ["LOGIN_LOGO_LIGHT"] = ("BRAND_LOGO_LOGIN_LIGHT", "Logo del inicio de sesión (modo claro)", true),
        ["LOGIN_LOGO_DARK"] = ("BRAND_LOGO_LOGIN_DARK", "Logo del inicio de sesión (modo oscuro)", true),
        ["REPORT_LOGO"] = ("BRAND_LOGO_REPORT", "Logo de reportes y documentos", false),
    };

    private readonly AppDbContext _db;
    private readonly IAppClock _clock;
    private readonly IInstitutionalLogoService _logo;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<BrandingService> _logger;

    public BrandingService(AppDbContext db, IAppClock clock, IInstitutionalLogoService logo, IWebHostEnvironment env, ILogger<BrandingService> logger)
    {
        _db = db;
        _clock = clock;
        _logo = logo;
        _env = env;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BrandAssetInfoDto>> ListAsync(CancellationToken ct)
    {
        var names = Keys.Values.Select(k => k.Param).ToList();
        var rows = await _db.Parameters.AsNoTracking().Where(p => names.Contains(p.Name)).ToListAsync(ct);
        return Keys.Select(k =>
        {
            var row = rows.FirstOrDefault(r => r.Name == k.Value.Param);
            var parsed = Parse(row?.Pvalues);
            return new BrandAssetInfoDto(k.Key, k.Value.Label, parsed is not null, FileNameOf(row?.Description), parsed?.ContentType, parsed is null ? null : Version(row!.Pvalues!), row?.UpdatedAt ?? row?.CreatedAt);
        }).ToList();
    }

    public async Task<BrandAssetContent?> GetContentAsync(string key, CancellationToken ct)
    {
        EnsureKey(key);
        var value = await _db.Parameters.AsNoTracking().Where(p => p.Name == Keys[key].Param).Select(p => p.Pvalues).FirstOrDefaultAsync(ct);
        var parsed = Parse(value);
        return parsed is null ? null : new BrandAssetContent(parsed.Value.Content, parsed.Value.ContentType, Version(value!));
    }

    public async Task<PublicBrandingDto> GetPublicAsync(CancellationToken ct)
    {
        var light = await GetContentAsync("LOGIN_LOGO_LIGHT", ct);
        var dark = await GetContentAsync("LOGIN_LOGO_DARK", ct);
        return new PublicBrandingDto(light?.Version, dark?.Version);
    }

    public async Task<BrandAssetInfoDto> SaveAsync(string key, string fileName, string contentType, byte[] content, int? userId, CancellationToken ct)
    {
        EnsureKey(key);
        var kind = ValidateImage(key, content);
        var name = Path.GetFileName(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name)) name = key.ToLowerInvariant() + (kind == "image/svg+xml" ? ".svg" : kind == "image/jpeg" ? ".jpg" : ".png");
        if (name.Length > 120) name = name[^120..];

        var (param, label, _) = Keys[key];
        var row = await _db.Parameters.FirstOrDefaultAsync(p => p.Name == param, ct);
        if (row is null)
        {
            row = new Parameters { Name = param, DataType = "TEXTO", IsActive = true };
            _db.Parameters.Add(row);
        }
        row.Pvalues = $"data:{kind};base64,{Convert.ToBase64String(content)}";
        row.Description = $"{label}. Vacío = logo oficial. (archivo: {name})";
        row.UpdatedAt = _clock.Now;
        row.UpdatedBy = userId;
        await _db.SaveChangesAsync(ct);

        if (key == "REPORT_LOGO") ApplyReportLogoToDisk(content, kind);
        return new BrandAssetInfoDto(key, label, true, name, kind, Version(row.Pvalues), row.UpdatedAt);
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        EnsureKey(key);
        var (param, label, _) = Keys[key];
        var row = await _db.Parameters.FirstOrDefaultAsync(p => p.Name == param, ct);
        if (row is null || row.Pvalues is null) return;
        row.Pvalues = null;
        row.Description = $"{label}. Vacío = logo oficial.";
        row.UpdatedAt = _clock.Now;
        await _db.SaveChangesAsync(ct);
        if (key == "REPORT_LOGO") ApplyReportLogoToDisk(null, null);
    }

    public async Task MaterializeReportLogoAsync(CancellationToken ct)
    {
        try
        {
            var value = await _db.Parameters.AsNoTracking().Where(p => p.Name == Keys["REPORT_LOGO"].Param).Select(p => p.Pvalues).FirstOrDefaultAsync(ct);
            var parsed = Parse(value);
            ApplyReportLogoToDisk(parsed?.Content, parsed?.ContentType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo preparar el logo de reportes; se usa el logo oficial.");
        }
    }

    private void ApplyReportLogoToDisk(byte[]? content, string? contentType)
    {
        try
        {
            if (content is null || contentType is null) BrandingFiles.DeleteReportLogo(_env.ContentRootPath);
            else BrandingFiles.WriteReportLogo(_env.ContentRootPath, content, contentType);
            _logo.Invalidate();
        }
        catch (Exception ex)
        {
            // El logo queda guardado en la BD; si el disco no es escribible los reportes siguen con el logo oficial.
            _logger.LogWarning(ex, "No se pudo escribir el logo de reportes en App_Data/branding.");
        }
    }

    /// <summary>Lee un data URI base64 (data:image/png;base64,...). Devuelve null si el valor está vacío o mal formado.</summary>
    private static (byte[] Content, string ContentType)? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("data:", StringComparison.Ordinal)) return null;
        var comma = value.IndexOf(',');
        var semi = value.IndexOf(';');
        if (comma < 0 || semi < 0 || semi > comma) return null;
        try { return (Convert.FromBase64String(value[(comma + 1)..]), value[5..semi]); }
        catch (FormatException) { return null; }
    }

    private static string Version(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();

    private static string? FileNameOf(string? description)
    {
        if (string.IsNullOrEmpty(description)) return null;
        var m = System.Text.RegularExpressions.Regex.Match(description, @"\(archivo: (.+)\)\s*$");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static void EnsureKey(string key)
    {
        if (!Keys.ContainsKey(key))
            throw new ArgumentException("Logo no reconocido. Use LOGIN_LOGO_LIGHT, LOGIN_LOGO_DARK o REPORT_LOGO.");
    }

    /// <summary>Valida tamaño y contenido real (no solo la extensión) y devuelve el tipo detectado.</summary>
    private static string ValidateImage(string key, byte[] content)
    {
        if (content.Length == 0) throw new ArgumentException("El archivo está vacío.");
        if (content.Length > MaxBytes) throw new ArgumentException("El logo no puede superar 1 MB. Reduzca su tamaño e inténtelo de nuevo.");

        string? type = null;
        if (content.Length > 8 && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47) type = "image/png";
        else if (content.Length > 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF) type = "image/jpeg";
        else if (Keys[key].AllowSvg)
        {
            var text = Encoding.UTF8.GetString(content.AsSpan(0, Math.Min(content.Length, content.Length))).TrimStart('\uFEFF', ' ', '\r', '\n', '\t');
            if (text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) || (text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) && text.Contains("<svg", StringComparison.OrdinalIgnoreCase)))
            {
                var lower = text.ToLowerInvariant();
                if (lower.Contains("<script") || lower.Contains("javascript:") || lower.Contains("<foreignobject") || System.Text.RegularExpressions.Regex.IsMatch(lower, @"\son[a-z]+\s*="))
                    throw new ArgumentException("El archivo SVG contiene elementos no permitidos (scripts o eventos). Exporte un SVG limpio solo con formas.");
                type = "image/svg+xml";
            }
        }
        if (type is null)
            throw new ArgumentException(Keys[key].AllowSvg
                ? "Formato no válido. Use una imagen PNG, JPEG o SVG."
                : "Formato no válido. Para reportes use una imagen PNG o JPEG (los PDF no admiten SVG).");
        return type;
    }
}

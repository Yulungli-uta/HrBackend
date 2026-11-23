namespace WsUtaSystem.Infrastructure.Services;

/// <summary>Ubicación en disco del logo de reportes personalizado (App_Data/branding). Los PDF lo leen de aquí si existe.</summary>
public static class BrandingFiles
{
    private static readonly string[] Extensions = { ".png", ".jpg" };

    public static string Directory(string contentRoot) => Path.Combine(contentRoot, "App_Data", "branding");

    /// <summary>Ruta del logo de reportes personalizado, o null si no hay (se usa el oficial).</summary>
    public static string? FindReportLogo(string contentRoot)
    {
        var dir = Directory(contentRoot);
        foreach (var ext in Extensions)
        {
            var path = Path.Combine(dir, "report-logo" + ext);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    public static void WriteReportLogo(string contentRoot, byte[] content, string contentType)
    {
        DeleteReportLogo(contentRoot);
        System.IO.Directory.CreateDirectory(Directory(contentRoot));
        var ext = contentType == "image/jpeg" ? ".jpg" : ".png";
        File.WriteAllBytes(Path.Combine(Directory(contentRoot), "report-logo" + ext), content);
    }

    public static void DeleteReportLogo(string contentRoot)
    {
        foreach (var ext in Extensions)
        {
            var path = Path.Combine(Directory(contentRoot), "report-logo" + ext);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}

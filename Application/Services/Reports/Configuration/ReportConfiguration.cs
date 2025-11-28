namespace WsUtaSystem.Application.Services.Reports.Configuration;

/// <summary>
/// Configuración global para reportes
/// </summary>
public class ReportConfiguration
{
    public ImageSettings Images { get; set; } = new();
    public int HeaderHeight { get; set; } = 80;
    public int FooterHeight { get; set; } = 40;
    public bool UseHeaderImage { get; set; } = true;
    public bool UseFooterImage { get; set; } = true;
    public string PageSize { get; set; } = "A4";
    public MarginSettings Margins { get; set; } = new();
    public ColorSettings Colors { get; set; } = new();
    public bool EnableAudit { get; set; } = true;
    public int MaxRecordsPerPage { get; set; } = 50;
}

public class ImageSettings
{
    public string HeaderPath { get; set; } = "wwwroot/images/reports/header.png";
    public string FooterPath { get; set; } = "wwwroot/images/reports/footer.png";
    public string LogoPath { get; set; } = "wwwroot/images/reports/logo.png";
}

public class MarginSettings
{
    public float Top { get; set; } = 20;
    public float Bottom { get; set; } = 20;
    public float Left { get; set; } = 20;
    public float Right { get; set; } = 20;
}

public class ColorSettings
{
    public string Primary { get; set; } = "#6c1313"; //"#1e3a8a";
    public string Secondary { get; set; } = "#3b82f6";
    public string Success { get; set; } = "#10b981";
    public string Warning { get; set; } = "#f59e0b";
    public string Danger { get; set; } = "#ef4444";
    public string TextPrimary { get; set; } = "#374151";
    public string TextSecondary { get; set; } = "#6b7280";
    public string Background { get; set; } = "#ffffff";
    public string AlternateRow { get; set; } = "#f3f4f6";
}

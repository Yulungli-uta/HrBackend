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
    public string LogoPath { get; set; } = "wwwroot/images/institutional/logo-uta.png"; // escudo oficial (mismo archivo que usan los demás documentos)
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
    public string Primary { get; set; } = "#00345e"; // Azul institucional UTA
    public string Secondary { get; set; } = "#ab171f"; // Rojo institucional UTA (acento y totales)
    public string Success { get; set; } = "#19703c";
    public string Warning { get; set; } = "#a35d05";
    public string Danger { get; set; } = "#ab171f";
    public string TextPrimary { get; set; } = "#1c2630";
    public string TextSecondary { get; set; } = "#536475";
    public string Background { get; set; } = "#ffffff";
    public string AlternateRow { get; set; } = "#eef2f6";
}

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Services.Reports.Configuration;

namespace WsUtaSystem.Application.Services.Reports.Generators.Base;

/// <summary>
/// Generador base para PDFs con soporte de imágenes de cabecera y pie
/// </summary>
public abstract class BasePdfGenerator
{
    protected readonly ReportConfiguration _config;
    protected readonly IWebHostEnvironment _env;

    protected BasePdfGenerator(
        ReportConfiguration config,
        IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
        
        // Configurar licencia de QuestPDF
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Compone la cabecera del reporte
    /// </summary>
    protected void ComposeHeader(IContainer container, string reportTitle, ReportFilterDto? filter, string userEmail)
    {
        container.Column(column =>
        {
            // Imagen de cabecera si existe
            var headerPath = GetHeaderImagePath();
            if (_config.UseHeaderImage && File.Exists(headerPath))
            {
                column.Item().Height(_config.HeaderHeight).Image(headerPath);
            }
            else
            {
                // Fallback: Cabecera de texto con logo
                column.Item().Row(row =>
                {
                    var logoPath = GetLogoImagePath();
                    if (File.Exists(logoPath))
                    {
                        row.ConstantItem(60).Height(60).Image(logoPath);
                    }
                    
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("UNIVERSIDAD TÉCNICA DE AMBATO")
                            .FontSize(16).Bold().FontColor(_config.Colors.Primary);
                        col.Item().Text("Sistema de Recursos Humanos")
                            .FontSize(12).FontColor(_config.Colors.TextSecondary);
                    });
                });
            }

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(_config.Colors.Primary);

            // Información del reporte
            column.Item().PaddingTop(10).Column(col =>
            {
                col.Item().Text(reportTitle)
                    .FontSize(18).Bold().FontColor(_config.Colors.Primary);
                
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(10).FontColor(_config.Colors.TextSecondary);
                    row.RelativeItem().Text($"Usuario: {userEmail}")
                        .FontSize(10).FontColor(_config.Colors.TextSecondary);
                });

                if (filter != null)
                {
                    var filterDesc = GetFilterDescription(filter);
                    if (!string.IsNullOrEmpty(filterDesc))
                    {
                        col.Item().PaddingTop(3).Text(filterDesc)
                            .FontSize(9).FontColor(_config.Colors.TextSecondary).Italic();
                    }
                }
            });
        });
    }

    /// <summary>
    /// Compone el pie de página del reporte
    /// </summary>
    protected void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(_config.Colors.Primary);
            
            var footerPath = GetFooterImagePath();
            if (_config.UseFooterImage && File.Exists(footerPath))
            {
                column.Item().Height(_config.FooterHeight).Image(footerPath);
            }
            else
            {
                // Fallback: Footer de texto
                column.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text("Sistema HR - Universidad Técnica de Ambato")
                        .FontSize(8).FontColor(_config.Colors.TextSecondary);
                    
                    row.ConstantItem(100).AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(8).FontColor(_config.Colors.TextSecondary);
                        text.CurrentPageNumber();
                        text.Span(" de ").FontSize(8).FontColor(_config.Colors.TextSecondary);
                        text.TotalPages();
                    });
                });
            }
        });
    }

    /// <summary>
    /// Obtiene la ruta completa de la imagen de cabecera
    /// </summary>
    protected string GetHeaderImagePath()
    {
        return Path.Combine(_env.ContentRootPath, _config.Images.HeaderPath);
    }

    /// <summary>
    /// Obtiene la ruta completa de la imagen de pie
    /// </summary>
    protected string GetFooterImagePath()
    {
        return Path.Combine(_env.ContentRootPath, _config.Images.FooterPath);
    }

    /// <summary>
    /// Obtiene la ruta completa del logo
    /// </summary>
    protected string GetLogoImagePath()
    {
        return Path.Combine(_env.ContentRootPath, _config.Images.LogoPath);
    }

    /// <summary>
    /// Genera descripción legible de los filtros aplicados
    /// </summary>
    protected virtual string GetFilterDescription(ReportFilterDto filter)
    {
        var parts = new List<string>();

        if (filter.StartDate.HasValue || filter.EndDate.HasValue)
        {
            if (filter.StartDate.HasValue && filter.EndDate.HasValue)
                parts.Add($"Período: {filter.StartDate:dd/MM/yyyy} - {filter.EndDate:dd/MM/yyyy}");
            else if (filter.StartDate.HasValue)
                parts.Add($"Desde: {filter.StartDate:dd/MM/yyyy}");
            else if (filter.EndDate.HasValue)
                parts.Add($"Hasta: {filter.EndDate:dd/MM/yyyy}");
        }

        if (filter.IsActive.HasValue)
            parts.Add($"Estado: {(filter.IsActive.Value ? "Activos" : "Inactivos")}");

        if (!string.IsNullOrEmpty(filter.EmployeeType))
            parts.Add($"Tipo: {filter.EmployeeType}");

        return parts.Any() ? $"Filtros: {string.Join(" | ", parts)}" : string.Empty;
    }
}

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WsUtaSystem.Application.Services.Reports.Configuration;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Renderers;

/// <summary>
/// Renderizador genérico de reportes en formato PDF usando QuestPDF.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: esta clase tiene una única responsabilidad — convertir una
/// <see cref="ReportDefinition"/> en bytes PDF. No conoce el origen de los datos.
/// </para>
/// <para>
/// Principio OCP: para agregar un nuevo reporte PDF no se modifica esta clase.
/// Solo se crea un nuevo <see cref="IReportSource"/> y se registra en DI.
/// </para>
/// <para>
/// Reutiliza la configuración de colores e imágenes de <see cref="ReportConfiguration"/>
/// para mantener la identidad visual corporativa en todos los reportes.
/// </para>
/// </remarks>
public sealed class PdfReportRenderer : IReportRenderer
{
    private readonly ReportConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PdfReportRenderer> _logger;

    /// <inheritdoc/>
    public ReportFormat Format => ReportFormat.Pdf;

    public PdfReportRenderer(
        ReportConfiguration config,
        IWebHostEnvironment env,
        ILogger<PdfReportRenderer> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _env    = env    ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configurar licencia de QuestPDF una sola vez
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Se lanza si la definición no contiene columnas válidas.
    /// </exception>
    public Task<byte[]> RenderAsync(ReportDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Columns is null || definition.Columns.Count == 0)
            throw new InvalidOperationException(
                $"La definición del reporte '{definition.Title}' no contiene columnas.");

        _logger.LogInformation(
            "PdfReportRenderer: generando PDF para '{Title}' con {Rows} filas y {Cols} columnas.",
            definition.Title,
            definition.Rows?.Count ?? 0,
            definition.Columns.Count);

        // Seleccionar tamaño de página según la orientación definida en el ReportDefinition.
        // Portrait: A4 210×297 mm | Landscape: A4 297×210 mm
        var pageSize = definition.Orientation == PageOrientation.Landscape
            ? PageSizes.A4.Landscape()
            : PageSizes.A4;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, definition));
                page.Content().Element(c => ComposeContent(c, definition));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();

        _logger.LogInformation(
            "PdfReportRenderer: PDF generado correctamente. Tamaño: {Size} bytes.", pdfBytes.Length);

        return Task.FromResult(pdfBytes);
    }

    // ─── Secciones del documento ──────────────────────────────────────────────

    private void ComposeHeader(IContainer container, ReportDefinition definition)
    {
        container.Column(column =>
        {
            // Imagen de cabecera corporativa si existe
            var headerPath = Path.Combine(_env.ContentRootPath, _config.Images.HeaderPath);
            if (_config.UseHeaderImage && File.Exists(headerPath))
            {
                column.Item().Height(_config.HeaderHeight).Image(headerPath);
            }
            else
            {
                // Fallback: cabecera de texto con logo
                column.Item().Row(row =>
                {
                    var logoPath = Path.Combine(_env.ContentRootPath, _config.Images.LogoPath);
                    if (File.Exists(logoPath))
                        row.ConstantItem(60).Height(60).Image(logoPath);

                    row.RelativeItem().Column(col =>
                    {
                        col.Item()
                            .Text("UNIVERSIDAD TÉCNICA DE AMBATO")
                            .FontSize(16).Bold().FontColor(_config.Colors.Primary);
                        col.Item()
                            .Text("Sistema de Recursos Humanos")
                            .FontSize(12).FontColor(_config.Colors.TextSecondary);
                    });
                });
            }

            column.Item().PaddingVertical(8).LineHorizontal(1).LineColor(_config.Colors.Primary);

            // Título y metadatos del reporte
            column.Item().PaddingTop(6).Column(col =>
            {
                col.Item()
                    .Text(definition.Title)
                    .FontSize(16).Bold().FontColor(_config.Colors.Primary);

                if (!string.IsNullOrWhiteSpace(definition.Subtitle))
                {
                    col.Item().PaddingTop(3)
                        .Text(definition.Subtitle)
                        .FontSize(10).Italic().FontColor(_config.Colors.TextSecondary);
                }

                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem()
                        .Text($"Generado: {definition.GeneratedAt.ToLocalTime():dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor(_config.Colors.TextSecondary);

                    if (!string.IsNullOrWhiteSpace(definition.GeneratedBy))
                    {
                        row.RelativeItem()
                            .Text($"Usuario: {definition.GeneratedBy}")
                            .FontSize(9).FontColor(_config.Colors.TextSecondary);
                    }
                });
            });
        });
    }

    //private void ComposeContent(IContainer container, ReportDefinition definition)
    //{
    //    container.PaddingTop(10).Table(table =>
    //    {
    //        // ── Definición de columnas ──────────────────────────────────────
    //        table.ColumnsDefinition(cols =>
    //        {
    //            foreach (var col in definition.Columns)
    //            {
    //                if (col.Width > 0f)
    //                    cols.ConstantColumn(col.Width);
    //                else
    //                    cols.RelativeColumn();
    //            }
    //        });

    //        // ── Fila de cabecera ────────────────────────────────────────────
    //        foreach (var col in definition.Columns)
    //        {
    //            table.Header(header =>
    //            {
    //                foreach (var c in definition.Columns)
    //                {
    //                    header.Cell()
    //                        .Background(_config.Colors.Primary)
    //                        .Padding(5)
    //                        .AlignCenter()
    //                        .Text(c.Header)
    //                        .FontSize(9).Bold().FontColor(Colors.White);
    //                }
    //            });
    //            break; // header se llama una sola vez
    //        }

    //        // ── Filas de datos ──────────────────────────────────────────────
    //        if (definition.Rows is null || definition.Rows.Count == 0)
    //        {
    //            table.Cell()
    //                .ColumnSpan((uint)definition.Columns.Count)
    //                .Padding(10)
    //                .AlignCenter()
    //                .Text("No se encontraron registros para los filtros aplicados.")
    //                .FontSize(10).Italic().FontColor(_config.Colors.TextSecondary);
    //            return;
    //        }

    //        var rowIndex = 0;
    //        foreach (var row in definition.Rows)
    //        {
    //            var isEven = rowIndex % 2 == 0;
    //            var bgColor = isEven ? Colors.White : Colors.Grey.Lighten5;

    //            foreach (var col in definition.Columns)
    //            {
    //                var value = row.TryGetValue(col.Key, out var v) ? v?.ToString() ?? string.Empty : string.Empty;

    //                var alignment = col.Alignment switch
    //                {
    //                    ColumnAlignment.Center => (Action<IContainer>)(c => c
    //                        .Background(bgColor).Padding(4).AlignCenter()
    //                        .Text(value).FontSize(8).FontColor(Colors.Black)),
    //                    ColumnAlignment.Right => (Action<IContainer>)(c => c
    //                        .Background(bgColor).Padding(4).AlignRight()
    //                        .Text(value).FontSize(8).FontColor(Colors.Black)),
    //                    _ => (Action<IContainer>)(c => c
    //                        .Background(bgColor).Padding(4).AlignLeft()
    //                        .Text(value).FontSize(8).FontColor(Colors.Black))
    //                };

    //                table.Cell().Element(alignment);
    //            }

    //            rowIndex++;
    //        }
    //    });
    //}

    private void ComposeContent(IContainer container, ReportDefinition definition)
    {
        container.PaddingTop(10).Table(table =>
        {
            // ── Definición de columnas ──────────────────────────────────────
            // Width ahora es OPCIONAL.
            // Si viene informado, se interpreta como peso relativo.
            // Si no viene o es <= 0, se usa 1f por defecto.
            table.ColumnsDefinition(cols =>
            {
                foreach (var col in definition.Columns)
                {
                    var relativeWidth = col.Width > 0f ? col.Width : 1f;
                    cols.RelativeColumn(relativeWidth);
                }
            });

            // ── Fila de cabecera ────────────────────────────────────────────
            table.Header(header =>
            {
                foreach (var c in definition.Columns)
                {
                    header.Cell()
                        .Background(_config.Colors.Primary)
                        .Border(0.5f)
                        .BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(5)
                        .PaddingHorizontal(4)
                        .AlignCenter()
                        .Text(c.Header)
                        .FontSize(9)
                        .Bold()
                        .FontColor(Colors.White);
                }
            });

            // ── Sin datos ───────────────────────────────────────────────────
            if (definition.Rows is null || definition.Rows.Count == 0)
            {
                table.Cell()
                    .ColumnSpan((uint)definition.Columns.Count)
                    .Padding(10)
                    .AlignCenter()
                    .Text("No se encontraron registros para los filtros aplicados.")
                    .FontSize(10)
                    .Italic()
                    .FontColor(_config.Colors.TextSecondary);

                return;
            }

            // ── Filas de datos ──────────────────────────────────────────────
            var rowIndex = 0;

            foreach (var row in definition.Rows)
            {
                var isEven = rowIndex % 2 == 0;
                var bgColor = isEven ? Colors.White : Colors.Grey.Lighten5;

                foreach (var col in definition.Columns)
                {
                    var value = row.TryGetValue(col.Key, out var v)
                        ? v?.ToString() ?? string.Empty
                        : string.Empty;

                    table.Cell().Element(cell =>
                    {
                        var styledCell = cell
                            .Background(bgColor)
                            .BorderBottom(0.5f)
                            .BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(4)
                            .PaddingHorizontal(4);

                        styledCell = col.Alignment switch
                        {
                            ColumnAlignment.Center => styledCell.AlignCenter(),
                            ColumnAlignment.Right => styledCell.AlignRight(),
                            _ => styledCell.AlignLeft()
                        };

                        styledCell.Text(value)
                            .FontSize(8)
                            .FontColor(Colors.Black);
                    });
                }

                rowIndex++;
            }
        });
    }
    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(_config.Colors.Primary);

            var footerPath = Path.Combine(_env.ContentRootPath, _config.Images.FooterPath);
            if (_config.UseFooterImage && File.Exists(footerPath))
            {
                column.Item().Height(_config.FooterHeight).Image(footerPath);
            }
            else
            {
                column.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem()
                        .Text("Sistema HR — Universidad Técnica de Ambato")
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
}

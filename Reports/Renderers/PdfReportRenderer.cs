using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WsUtaSystem.Application.Interfaces.Services;
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
    private readonly IInstitutionalLogoService _logoService;
    private readonly ILogger<PdfReportRenderer> _logger;

    /// <inheritdoc/>
    public ReportFormat Format => ReportFormat.Pdf;

    public PdfReportRenderer(
        ReportConfiguration config,
        IWebHostEnvironment env,
        IInstitutionalLogoService logoService,
        ILogger<PdfReportRenderer> logger)
    {
        _config      = config      ?? throw new ArgumentNullException(nameof(config));
        _env         = env         ?? throw new ArgumentNullException(nameof(env));
        _logoService = logoService ?? throw new ArgumentNullException(nameof(logoService));
        _logger      = logger      ?? throw new ArgumentNullException(nameof(logger));

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

        var plan = BuildPagePlan(definition, pageSize);
        pageSize = plan.PageSize;

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(pageSize);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, definition));
                page.Content().Element(c => ComposeContent(c, definition, plan.Blocks));
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
                    var logoPath = _logoService.GetLogoFilePath();
                    if (logoPath is not null)
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

    /// <summary>
    /// Renderiza el contenido de una celda de cabecera (fondo, borde, texto), en horizontal
    /// o rotado 90° según <paramref name="vertical"/>. Extraído para reutilizar tanto en la
    /// variante que se repite en cada página como en la que aparece solo una vez.
    /// </summary>
    private void RenderHeaderCell(IContainer cell, string headerText, bool vertical)
    {
        var styled = cell
            .Background(_config.Colors.Primary)
            .Border(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(vertical ? 3 : 5)
            .PaddingHorizontal(vertical ? 2 : 4)
            .MinHeight(vertical ? 90 : 0)
            .AlignCenter()
            .AlignMiddle();

        if (vertical)
        {
            styled.RotateLeft()
                .Text(headerText)
                .FontSize(8).Bold().FontColor(Colors.White);
        }
        else
        {
            styled.Text(headerText)
                .FontSize(9).Bold().FontColor(Colors.White);
        }
    }

    /// <summary>
    /// Contenido del PDF: uno o más bloques de columnas, cada uno con todas las filas. Con un solo bloque (lo normal) es la tabla
    /// de siempre; con varios, cada bloque empieza en una hoja nueva y repite las columnas fijas.
    /// </summary>
    private void ComposeContent(IContainer container, ReportDefinition definition, IReadOnlyList<TableBlock> blocks)
    {
        container.PaddingTop(10).Column(column =>
        {
            for (var i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (i > 0) column.Item().PageBreak();

                if (block.Caption is not null)
                {
                    column.Item().PaddingBottom(4)
                        .Text(block.Caption)
                        .FontSize(9).Bold().FontColor(_config.Colors.TextSecondary);
                }

                column.Item().Element(c => ComposeTable(c, definition, block));
            }
        });
    }

    private void ComposeTable(IContainer container, ReportDefinition definition, TableBlock block)
    {
        var columns = block.Columns;
        var columnWidths = block.Widths;

        container.Table(table =>
        {
            // ── Definición de columnas ──────────────────────────────────────
            // Width ahora es OPCIONAL.
            // Si viene informado, se interpreta como peso relativo.
            // Si no viene o es <= 0, se usa 1f por defecto.
            table.ColumnsDefinition(cols =>
            {
                for (var i = 0; i < columns.Count; i++)
                {
                    if (columnWidths is not null)
                    {
                        cols.ConstantColumn(columnWidths[i]);
                        continue;
                    }

                    var relativeWidth = columns[i].Width > 0f ? columns[i].Width : 1f;
                    cols.RelativeColumn(relativeWidth);
                }
            });

            // ── Fila de cabecera ────────────────────────────────────────────
            // RepeatHeaderOnEveryPage=true (por defecto, comportamiento histórico): usa
            // table.Header(...), que QuestPDF repite automáticamente en cada página.
            // RepeatHeaderOnEveryPage=false: se renderiza como una fila normal de contenido,
            // una sola vez, al inicio del documento.
            if (definition.RepeatHeaderOnEveryPage)
            {
                table.Header(header =>
                {
                    foreach (var c in columns)
                        header.Cell().Element(cell => RenderHeaderCell(cell, c.Header, definition.VerticalHeaders));
                });
            }
            else
            {
                foreach (var c in columns)
                    table.Cell().Element(cell => RenderHeaderCell(cell, c.Header, definition.VerticalHeaders));
            }

            // ── Sin datos ───────────────────────────────────────────────────
            if (definition.Rows is null || definition.Rows.Count == 0)
            {
                table.Cell()
                    .ColumnSpan((uint)columns.Count)
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

                foreach (var col in columns)
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
    // ─── Paginación de columnas ───────────────────────────────────────────────

    /// <summary>Puntos por carácter (mayúsculas a 8 pt) y relleno horizontal de la celda (4 + 4 pt).</summary>
    private const float PointsPerChar = 5.2f;
    private const float HeaderPointsPerChar = 5.8f;
    private const float CellPadding = 10f;
    private const int MaxContentChars = 40;
    private const int MaxHeaderChars = 30;
    private const float PageMargin = 42.5f; // 1.5 cm

    /// <summary>Holgura: si las columnas suman exactamente el ancho útil, QuestPDF lo trata como restricción en conflicto.</summary>
    private const float LayoutSlack = 0.995f;
    private const float PageWidthSlack = 6f;

    /// <summary>Tope de ancho de página que abren los visores PDF (200 pulgadas).</summary>
    private const float MaxPageWidth = 14400f;

    /// <summary>Ancho mínimo que debe quedar para las columnas del bloque tras reservar las fijas; si no, se ignoran las fijas.</summary>
    private const float MinBlockWidth = 160f;

    /// <summary>Con más columnas que esto, un reporte en <see cref="PdfPagination.FitA4"/> que no cabe legible deja un aviso en el log.</summary>
    private const int CrampedWarningColumns = 15;

    /// <summary>Un bloque de columnas con todas las filas. <c>Widths</c> null = anchos relativos de siempre (hoja A4 completa).</summary>
    private sealed record TableBlock(IReadOnlyList<ReportColumn> Columns, IReadOnlyList<float>? Widths, string? Caption);

    private sealed record PagePlan(PageSize PageSize, IReadOnlyList<TableBlock> Blocks);

    /// <summary>
    /// Decide el tamaño de hoja y cómo se reparten las columnas según <see cref="ReportDefinition.PdfPagination"/>.
    /// Por defecto (FitA4) no cambia nada respecto al comportamiento histórico.
    /// </summary>
    private PagePlan BuildPagePlan(ReportDefinition definition, PageSize basePage)
    {
        var single = new PagePlan(basePage, [new TableBlock(definition.Columns, null, null)]);
        var usable = basePage.Width - 2 * PageMargin;

        if (definition.PdfPagination == PdfPagination.FitA4)
        {
            WarnIfCramped(definition, usable);
            return single;
        }

        var measured = MeasureColumns(definition);
        var total = measured.Sum();

        // Si cabe legible en una A4 no hace falta partir ni extender nada.
        if (total <= usable) return single;

        return definition.PdfPagination == PdfPagination.Expand
            ? BuildExpandedPlan(definition, measured, basePage)
            : BuildBlocksPlan(definition, measured, basePage);
    }

    /// <summary>Una sola hoja tan ancha como haga falta (misma altura que la A4).</summary>
    private static PagePlan BuildExpandedPlan(ReportDefinition definition, float[] measured, PageSize basePage)
    {
        var widths = (float[])measured.Clone();
        var pageWidth = Math.Min(widths.Sum() + 2 * PageMargin + PageWidthSlack, MaxPageWidth);
        if (widths.Sum() + 2 * PageMargin + PageWidthSlack > MaxPageWidth)
        {
            var scale = (MaxPageWidth - 2 * PageMargin) / widths.Sum() * LayoutSlack;
            for (var i = 0; i < widths.Length; i++) widths[i] *= scale;
        }

        return new PagePlan(new PageSize(pageWidth, basePage.Height), [new TableBlock(definition.Columns, widths, null)]);
    }

    /// <summary>
    /// Parte las columnas en bloques que caben legibles en el ancho útil de la hoja base. Las columnas fijas
    /// (<see cref="ReportDefinition.PinnedColumnKeys"/>) se muestran al inicio de cada bloque.
    /// </summary>
    private static PagePlan BuildBlocksPlan(ReportDefinition definition, float[] measured, PageSize basePage)
    {
        var usable = (basePage.Width - 2 * PageMargin) * LayoutSlack;
        var indexByKey = definition.Columns
            .Select((c, i) => (c.Key, i))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().i);

        var pinned = definition.PinnedColumnKeys
            .Where(indexByKey.ContainsKey)
            .Select(k => indexByKey[k])
            .Distinct()
            .ToList();

        // Si las fijas ocupan casi toda la hoja, se renuncia a repetirlas.
        if (pinned.Sum(i => measured[i]) > usable - MinBlockWidth) pinned.Clear();

        var pinnedSet = pinned.ToHashSet();
        var pinnedWidth = pinned.Sum(i => measured[i]);
        var available = usable - pinnedWidth;

        var groups = new List<List<int>>();
        var current = new List<int>();
        var currentWidth = 0f;
        for (var i = 0; i < definition.Columns.Count; i++)
        {
            if (pinnedSet.Contains(i)) continue;
            var width = Math.Min(measured[i], available);
            if (current.Count > 0 && currentWidth + width > available)
            {
                groups.Add(current);
                current = [];
                currentWidth = 0f;
            }

            current.Add(i);
            currentWidth += width;
        }

        if (current.Count > 0) groups.Add(current);
        if (groups.Count == 0) return new PagePlan(basePage, [new TableBlock(definition.Columns, null, null)]);

        var blocks = new List<TableBlock>(groups.Count);
        for (var g = 0; g < groups.Count; g++)
        {
            var indices = pinned.Concat(groups[g]).ToList();
            var widths = indices.Select(i => Math.Min(measured[i], available + (pinned.Contains(i) ? measured[i] : 0f))).ToArray();

            // Reparte el ancho sobrante entre las columnas del bloque (sin estirar más de 1,35 veces).
            var sum = widths.Sum();
            var scale = Math.Min(usable / sum, 1.35f);
            for (var i = 0; i < widths.Length; i++) widths[i] *= scale;

            var caption = $"Bloque {g + 1} de {groups.Count}";
            blocks.Add(new TableBlock(indices.Select(i => definition.Columns[i]).ToList(), widths, caption));
        }

        return new PagePlan(basePage, blocks);
    }

    /// <summary>Ancho que necesita cada columna (con relleno) según su encabezado y el percentil 95 de su contenido.</summary>
    private static float[] MeasureColumns(ReportDefinition definition)
    {
        var widths = new float[definition.Columns.Count];
        for (var i = 0; i < widths.Length; i++)
        {
            var col = definition.Columns[i];

            // Percentil 95 del largo del contenido: unas pocas celdas largas se parten en dos líneas en vez de ensanchar toda la columna.
            var lengths = new List<int>();
            if (definition.Rows is not null)
            {
                foreach (var row in definition.Rows)
                {
                    if (row.TryGetValue(col.Key, out var v) && v is not null)
                        lengths.Add(v.ToString()?.Length ?? 0);
                }
            }

            lengths.Sort();
            var contentChars = lengths.Count == 0 ? 0 : lengths[(int)Math.Floor((lengths.Count - 1) * 0.95)];

            var contentWidth = Math.Min(contentChars, MaxContentChars) * PointsPerChar;
            var headerWidth = definition.VerticalHeaders ? 0f : Math.Min(col.Header.Length, MaxHeaderChars) * HeaderPointsPerChar;
            widths[i] = Math.Max(contentWidth, headerWidth) + CellPadding;
        }

        return widths;
    }

    /// <summary>
    /// Un reporte con muchas columnas en FitA4 que no caben legibles no se cambia (cada reporte decide su formato), pero queda
    /// constancia en el log para que quien lo mantenga considere <see cref="PdfPagination.Blocks"/>.
    /// </summary>
    private void WarnIfCramped(ReportDefinition definition, float usable)
    {
        if (definition.Columns.Count <= CrampedWarningColumns) return;

        var needed = MeasureColumns(definition).Sum();
        if (needed > usable)
        {
            _logger.LogWarning(
                "PdfReportRenderer: '{Title}' tiene {Cols} columnas que necesitan ~{Needed:F0} pt y la hoja ofrece {Usable:F0} pt; " +
                "se imprime apretado en A4. Considere ReportDefinition.PdfPagination = Blocks.",
                definition.Title, definition.Columns.Count, needed, usable);
        }
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

using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WsUtaSystem.Application.Services.Reports.Configuration;
using WsUtaSystem.Documents.Abstractions;
using WsUtaSystem.Models;

namespace WsUtaSystem.Documents.Renderers;

/// <summary>
/// Renderer de documentos institucionales (Acción de Personal, Contratos, Oficios)
/// usando QuestPDF con layout programático.
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: esta clase tiene una única responsabilidad — convertir un
/// <see cref="DocumentTemplate"/> con valores resueltos en bytes PDF.
/// No conoce el origen de los datos ni la lógica de negocio.
/// </para>
/// <para>
/// Principio OCP: para agregar nuevos tipos de documento no se modifica esta clase.
/// El layout se adapta dinámicamente a los campos y secciones de la plantilla.
/// </para>
/// </remarks>
public sealed class InstitutionalDocumentRenderer : IDocumentRenderer
{
    private readonly ReportConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<InstitutionalDocumentRenderer> _logger;

    // Colores institucionales UTA
    private static readonly string PrimaryColor   = "#8B1A1A";  // Guinda UTA
    private static readonly string SecondaryColor = "#F5F5F5";  // Gris claro
    private static readonly string BorderColor    = "#CCCCCC";
    private static readonly string TextColor      = "#1A1A1A";

    public InstitutionalDocumentRenderer(
        ReportConfiguration config,
        IWebHostEnvironment env,
        ILogger<InstitutionalDocumentRenderer> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _env    = env    ?? throw new ArgumentNullException(nameof(env));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public Task<byte[]> RenderToPdfAsync(string htmlContent, string? cssStyles = null)
    {
        // htmlContent contiene el HTML ya procesado con placeholders sustituidos.
        // Lo parseamos para extraer secciones y renderizamos con QuestPDF.
        _logger.LogInformation(
            "InstitutionalDocumentRenderer: iniciando generación de PDF ({Chars} chars).",
            htmlContent.Length);

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x
                    .FontSize(10)
                    .FontColor(TextColor));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, htmlContent));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();

        _logger.LogInformation(
            "InstitutionalDocumentRenderer: PDF generado ({Bytes} bytes).",
            pdfBytes.Length);

        return Task.FromResult(pdfBytes);
    }

    // ── Secciones del documento ──────────────────────────────────────────────────

    private void ComposeHeader(IContainer container)
    {
        var logoPath = Path.Combine(_env.WebRootPath ?? string.Empty, _config.Images.LogoPath ?? string.Empty);
        var hasLogo  = !string.IsNullOrEmpty(_config.Images.LogoPath) && File.Exists(logoPath);

        container.BorderBottom(1).BorderColor(PrimaryColor).PaddingBottom(8).Row(row =>
        {
            // Logo institucional
            if (hasLogo)
            {
                row.ConstantItem(80).Image(logoPath).FitArea();
            }
            else
            {
                row.ConstantItem(80).AlignCenter().AlignMiddle()
                    .Text("UTA").Bold().FontSize(18).FontColor(PrimaryColor);
            }

            row.RelativeItem().PaddingLeft(10).Column(col =>
            {
                col.Item().Text("Universidad Técnica de Ambato")
                    .Bold().FontSize(13).FontColor(PrimaryColor);
                col.Item().Text("Sistema de Recursos Humanos")
                    .FontSize(9).FontColor("#555555");
            });

            row.ConstantItem(100).AlignRight().AlignBottom()
                .Text($"Fecha: {DateTime.Now:dd/MM/yyyy}")
                .FontSize(8).FontColor("#777777");
        });
    }

    private static void ComposeContent(IContainer container, string htmlContent)
    {
        // Parseamos el HTML procesado para extraer el contenido estructurado.
        // Usamos un parser simple basado en secciones delimitadas por comentarios HTML.
        container.PaddingTop(10).Column(col =>
        {
            col.Spacing(6);

            // Título del documento
            var title = ExtractMetaValue(htmlContent, "DOCUMENT_TITLE");
            if (!string.IsNullOrEmpty(title))
            {
                col.Item().AlignCenter().Text(title)
                    .Bold().FontSize(14).FontColor("#1A1A1A");
            }

            // Número de acción
            var actionNumber = ExtractMetaValue(htmlContent, "ACTION_NUMBER");
            if (!string.IsNullOrEmpty(actionNumber))
            {
                col.Item().AlignCenter().Text($"N° {actionNumber}")
                    .FontSize(11).FontColor("#555555");
            }

            col.Item().PaddingTop(6);

            // Cuerpo del documento — renderizamos el HTML como texto estructurado
            RenderHtmlBody(col, htmlContent);
        });
    }

    private static void RenderHtmlBody(ColumnDescriptor col, string htmlContent)
    {
        // Extraer el cuerpo del documento entre <body> tags
        var bodyStart = htmlContent.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
        var bodyEnd   = htmlContent.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);

        var body = (bodyStart >= 0 && bodyEnd > bodyStart)
            ? htmlContent[(bodyStart + 6)..bodyEnd]
            : htmlContent;

        // Procesar secciones de tabla (<table class="doc-section">)
        var sections = ExtractSections(body);

        foreach (var section in sections)
        {
            col.Item().Element(c => RenderSection(c, section));
        }
    }

    private static void RenderSection(IContainer container, DocumentSection section)
    {
        container.Column(col =>
        {
            col.Spacing(4);

            if (!string.IsNullOrEmpty(section.Title))
            {
                col.Item()
                    .Background(PrimaryColor)
                    .Padding(4)
                    .Text(section.Title)
                    .Bold().FontSize(9).FontColor("#FFFFFF");
            }

            if (section.Rows.Count > 0)
            {
                col.Item().Border(1).BorderColor(BorderColor).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(2);
                    });

                    foreach (var (label, value) in section.Rows)
                    {
                        table.Cell()
                            .Background(SecondaryColor)
                            .Padding(4)
                            .Text(label)
                            .Bold().FontSize(9);

                        table.Cell()
                            .Padding(4)
                            .Text(value ?? string.Empty)
                            .FontSize(9);
                    }
                });
            }

            if (!string.IsNullOrEmpty(section.FreeText))
            {
                col.Item().Padding(4).Text(section.FreeText).FontSize(9);
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(BorderColor).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text("Sistema HR — Universidad Técnica de Ambato")
                .FontSize(7).FontColor("#777777");
            row.ConstantItem(80).AlignRight()
                .Text(x =>
                {
                    x.Span("Página ").FontSize(7).FontColor("#777777");
                    x.CurrentPageNumber().FontSize(7).FontColor("#777777");
                    x.Span(" de ").FontSize(7).FontColor("#777777");
                    x.TotalPages().FontSize(7).FontColor("#777777");
                });
        });
    }

    // ── Helpers de parsing HTML ──────────────────────────────────────────────────

    private static string? ExtractMetaValue(string html, string metaName)
    {
        var pattern = $@"<meta\s+name=""{metaName}""\s+content=""([^""]*)""\s*/>";
        var match   = System.Text.RegularExpressions.Regex.Match(
            html, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static List<DocumentSection> ExtractSections(string body)
    {
        var sections = new List<DocumentSection>();

        // Buscar secciones delimitadas por <!-- SECTION: Título -->
        var sectionPattern = new System.Text.RegularExpressions.Regex(
            @"<!--\s*SECTION:\s*([^-]*?)\s*-->(.*?)(?=<!--\s*SECTION:|$)",
            System.Text.RegularExpressions.RegexOptions.Singleline |
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var matches = sectionPattern.Matches(body);

        if (matches.Count == 0)
        {
            // Sin secciones explícitas: tratar todo el body como texto libre
            sections.Add(new DocumentSection
            {
                Title    = null,
                FreeText = StripHtmlTags(body).Trim()
            });
            return sections;
        }

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var section = new DocumentSection
            {
                Title = match.Groups[1].Value.Trim()
            };

            var sectionBody = match.Groups[2].Value;

            // Extraer filas de tabla <tr><td>Label</td><td>Value</td></tr>
            var rowPattern = new System.Text.RegularExpressions.Regex(
                @"<tr[^>]*>\s*<td[^>]*>(.*?)</td>\s*<td[^>]*>(.*?)</td>\s*</tr>",
                System.Text.RegularExpressions.RegexOptions.Singleline |
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            var rowMatches = rowPattern.Matches(sectionBody);
            foreach (System.Text.RegularExpressions.Match row in rowMatches)
            {
                section.Rows.Add((
                    StripHtmlTags(row.Groups[1].Value).Trim(),
                    StripHtmlTags(row.Groups[2].Value).Trim()
                ));
            }

            if (section.Rows.Count == 0)
            {
                section.FreeText = StripHtmlTags(sectionBody).Trim();
            }

            sections.Add(section);
        }

        return sections;
    }

    private static string StripHtmlTags(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ")
            .Replace("&nbsp;", " ")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Trim();
    }

    // ── Modelos internos de parsing ──────────────────────────────────────────────

    private sealed class DocumentSection
    {
        public string? Title    { get; set; }
        public string? FreeText { get; set; }
        public List<(string Label, string Value)> Rows { get; } = [];
    }
}

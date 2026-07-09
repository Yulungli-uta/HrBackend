using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using WsUtaSystem.Reports.Abstractions;

namespace WsUtaSystem.Reports.Renderers
{
    public sealed class HtmlDocumentRenderer : IDocumentRenderer
    {
        private readonly ILogger<HtmlDocumentRenderer> _logger;

        // Convención genérica: cualquier plantilla puede declarar
        // <meta name="DOCUMENT_CODE" content="..."/> en su <head> para que el footer del PDF
        // muestre el código/número del documento (contrato, acción de personal, etc.) sin que
        // este renderer necesite saber de qué tipo de documento se trata.
        private static readonly Regex DocumentCodeMetaPattern = new(
            "<meta\\s+name=\"DOCUMENT_CODE\"\\s+content=\"([^\"]*)\"\\s*/?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public HtmlDocumentRenderer(ILogger<HtmlDocumentRenderer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<byte[]> RenderToPdfAsync(string htmlContent, string? cssStyles = null)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                throw new ArgumentException("El contenido HTML no puede estar vacío.", nameof(htmlContent));

            var html = BuildHtmlDocument(htmlContent, cssStyles);

            _logger.LogInformation(
                "HtmlDocumentRenderer: iniciando generación PDF desde HTML. Chars={Chars}",
                html.Length);

            await EnsureChromiumAsync();

            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args =
                [
                    "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-gpu"
                ]
            });

            await using var page = await browser.NewPageAsync();

            await page.SetContentAsync(html, new PuppeteerSharp.NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle0],
                Timeout = 60000
            });

            var pdfBytes = await page.PdfDataAsync(new PdfOptions
            {
                Format = PuppeteerSharp.Media.PaperFormat.A4,
                PrintBackground = true,
                PreferCSSPageSize = true,
                DisplayHeaderFooter = true,
                // Header vacío explícito: si no se especifica, Chromium dibuja su propio
                // encabezado por defecto (fecha + título + URL), que no queremos en el PDF.
                HeaderTemplate = "<span></span>",
                FooterTemplate = BuildFooterTemplate(ExtractDocumentCode(html)),
                MarginOptions = new PuppeteerSharp.Media.MarginOptions
                {
                    Top = "0mm",
                    Right = "0mm",
                    Bottom = "12mm",
                    Left = "0mm"
                }
            });

            _logger.LogInformation(
                "HtmlDocumentRenderer: PDF generado correctamente. Bytes={Bytes}",
                pdfBytes.Length);

            return pdfBytes;
        }

        private static string BuildHtmlDocument(string htmlContent, string? cssStyles)
        {
            if (htmlContent.Contains("<html", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(cssStyles))
                    return htmlContent;

                return htmlContent.Replace(
                    "</head>",
                    $"<style>{cssStyles}</style></head>",
                    StringComparison.OrdinalIgnoreCase);
            }

            var builder = new StringBuilder();

            builder.AppendLine("<!DOCTYPE html>");
            builder.AppendLine("<html lang=\"es\">");
            builder.AppendLine("<head>");
            builder.AppendLine("<meta charset=\"UTF-8\"/>");
            builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"/>");

            if (!string.IsNullOrWhiteSpace(cssStyles))
            {
                builder.AppendLine("<style>");
                builder.AppendLine(cssStyles);
                builder.AppendLine("</style>");
            }

            builder.AppendLine("</head>");
            builder.AppendLine("<body>");
            builder.AppendLine(htmlContent);
            builder.AppendLine("</body>");
            builder.AppendLine("</html>");

            return builder.ToString();
        }

        private static string? ExtractDocumentCode(string html)
        {
            var match = DocumentCodeMetaPattern.Match(html);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>
        /// Footer genérico para cualquier documento: numeración de página (calculada por
        /// Chromium, siempre correcta sin importar el largo del documento) + el código del
        /// documento si la plantilla lo declaró vía <c>DOCUMENT_CODE</c>.
        /// </summary>
        private static string BuildFooterTemplate(string? documentCode)
        {
            var codeSuffix = string.IsNullOrWhiteSpace(documentCode)
                ? string.Empty
                : $" &mdash; {WebUtility.HtmlEncode(documentCode)}";

            return $"<div style=\"width:100%;font-size:8px;color:#666;font-family:Arial,Helvetica,sans-serif;text-align:center;\">" +
                   $"Página <span class=\"pageNumber\"></span> de <span class=\"totalPages\"></span>{codeSuffix}</div>";
        }

        private static async Task EnsureChromiumAsync()
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
        }
    }
}

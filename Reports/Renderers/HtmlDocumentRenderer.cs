using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.Text;
using WsUtaSystem.Reports.Abstractions;

namespace WsUtaSystem.Reports.Renderers
{
    public sealed class HtmlDocumentRenderer : IDocumentRenderer
    {
        private readonly ILogger<HtmlDocumentRenderer> _logger;

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
                MarginOptions = new PuppeteerSharp.Media.MarginOptions
                {
                    Top = "0mm",
                    Right = "0mm",
                    Bottom = "0mm",
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

        private static async Task EnsureChromiumAsync()
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();
        }
    }
}

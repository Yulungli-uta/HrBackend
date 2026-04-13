using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;

namespace WsUtaSystem.Reports.Services;

/// <summary>
/// Implementación del servicio central genérico de generación de reportes (v2).
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: este servicio tiene una única responsabilidad — orquestar la
/// selección del <see cref="IReportSource"/> correcto, construir la
/// <see cref="ReportDefinition"/> y delegar el renderizado al <see cref="IReportRenderer"/>
/// correspondiente. No contiene lógica de negocio de ningún reporte específico.
/// </para>
/// <para>
/// Principio OCP: agregar un nuevo reporte no requiere modificar esta clase.
/// Solo se necesita registrar un nuevo <see cref="IReportSource"/> en DI.
/// </para>
/// <para>
/// Principio DIP: depende de las abstracciones <see cref="IReportSource"/> e
/// <see cref="IReportRenderer"/>, no de implementaciones concretas.
/// </para>
/// <para>
/// Principio LSP: cualquier implementación de <see cref="IReportSource"/> o
/// <see cref="IReportRenderer"/> puede ser sustituida sin afectar este servicio.
/// </para>
/// </remarks>
public sealed class ReportServiceV2 : IReportServiceV2
{
    private readonly IReadOnlyDictionary<ReportType, IReportSource>   _sources;
    private readonly IReadOnlyDictionary<ReportFormat, IReportRenderer> _renderers;
    private readonly ILogger<ReportServiceV2> _logger;

    public ReportServiceV2(
        IEnumerable<IReportSource>   sources,
        IEnumerable<IReportRenderer> renderers,
        ILogger<ReportServiceV2>     logger)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(renderers);
        ArgumentNullException.ThrowIfNull(logger);

        // Indexar por tipo/formato para búsqueda O(1)
        _sources   = sources.ToDictionary(s => s.ReportType);
        _renderers = renderers.ToDictionary(r => r.Format);
        _logger    = logger;

        _logger.LogInformation(
            "ReportServiceV2 inicializado. Sources registrados: [{Sources}]. Renderers registrados: [{Renderers}].",
            string.Join(", ", _sources.Keys),
            string.Join(", ", _renderers.Keys));
    }

    /// <inheritdoc/>
    public Task<byte[]> GeneratePdfAsync(
        ReportType reportType,
        ReportFilterDto filter,
        HttpContext context)
        => GenerateAsync(reportType, ReportFormat.Pdf, filter, context);

    /// <inheritdoc/>
    public Task<byte[]> GenerateExcelAsync(
        ReportType reportType,
        ReportFilterDto filter,
        HttpContext context)
        => GenerateAsync(reportType, ReportFormat.Excel, filter, context);

    /// <inheritdoc/>
    public async Task<byte[]> GenerateAsync(
        ReportType      reportType,
        ReportFormat    format,
        ReportFilterDto filter,
        HttpContext      context)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "ReportServiceV2.GenerateAsync: ReportType={ReportType}, Format={Format}.",
            reportType, format);

        // ── 1. Seleccionar el source ──────────────────────────────────────────
        if (!_sources.TryGetValue(reportType, out var source))
        {
            var available = string.Join(", ", _sources.Keys);
            throw new InvalidOperationException(
                $"No existe un IReportSource registrado para el tipo '{reportType}'. " +
                $"Sources disponibles: [{available}].");
        }

        // ── 2. Seleccionar el renderer ────────────────────────────────────────
        if (!_renderers.TryGetValue(format, out var renderer))
        {
            var available = string.Join(", ", _renderers.Keys);
            throw new InvalidOperationException(
                $"No existe un IReportRenderer registrado para el formato '{format}'. " +
                $"Renderers disponibles: [{available}].");
        }

        // ── 3. Construir la definición del reporte ────────────────────────────
        _logger.LogDebug(
            "ReportServiceV2: invocando source '{SourceType}' para construir la definición.",
            source.GetType().Name);

        var definition = await source.BuildAsync(filter, context);

        _logger.LogInformation(
            "ReportServiceV2: definición construida. Título='{Title}', Filas={Rows}, Columnas={Cols}.",
            definition.Title,
            definition.Rows?.Count ?? 0,
            definition.Columns?.Count ?? 0);

        // ── 4. Renderizar y devolver bytes ────────────────────────────────────
        _logger.LogDebug(
            "ReportServiceV2: invocando renderer '{RendererType}'.",
            renderer.GetType().Name);

        var bytes = await renderer.RenderAsync(definition);

        _logger.LogInformation(
            "ReportServiceV2: reporte generado. Formato={Format}, Tamaño={Size} bytes.",
            format, bytes.Length);

        return bytes;
    }
}

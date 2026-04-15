using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Reports.Common;
using WsUtaSystem.Application.Interfaces.Reports;
using WsUtaSystem.Middleware;
using WsUtaSystem.Reports.Abstractions;
using WsUtaSystem.Reports.Core;
using WsUtaSystem.Reports.Helpers;

namespace WsUtaSystem.Reports.Services;

/// <summary>
/// Implementación del servicio central genérico de generación de reportes (v2).
/// </summary>
/// <remarks>
/// <para>
/// Principio SRP: este servicio tiene una única responsabilidad — orquestar la
/// selección del <see cref="IReportSource"/> correcto, construir la
/// <see cref="ReportDefinition"/> y delegar el renderizado al <see cref="IReportRenderer"/>
/// correspondiente. La auditoría se registra automáticamente en el bloque <c>finally</c>
/// sin que ningún <see cref="IReportSource"/> deba conocer su existencia.
/// </para>
/// <para>
/// Principio OCP: agregar un nuevo reporte no requiere modificar esta clase.
/// Solo se necesita registrar un nuevo <see cref="IReportSource"/> en DI.
/// La auditoría se aplica automáticamente a todos los reportes presentes y futuros.
/// </para>
/// <para>
/// Principio DIP: depende de las abstracciones <see cref="IReportSource"/>,
/// <see cref="IReportRenderer"/> e <see cref="IReportAuditService"/>,
/// no de implementaciones concretas.
/// </para>
/// <para>
/// Principio LSP: cualquier implementación de <see cref="IReportSource"/> o
/// <see cref="IReportRenderer"/> puede ser sustituida sin afectar este servicio.
/// </para>
/// </remarks>
public sealed class ReportServiceV2 : IReportServiceV2
{
    private readonly IReadOnlyDictionary<ReportType,   IReportSource>   _sources;
    private readonly IReadOnlyDictionary<ReportFormat, IReportRenderer> _renderers;
    private readonly IReportAuditService                                 _auditService;
    private readonly ILogger<ReportServiceV2>                            _logger;

    public ReportServiceV2(
        IEnumerable<IReportSource>   sources,
        IEnumerable<IReportRenderer> renderers,
        IReportAuditService          auditService,
        ILogger<ReportServiceV2>     logger)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(renderers);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(logger);

        // Indexar por tipo/formato para búsqueda O(1)
        _sources      = sources.ToDictionary(s => s.ReportType);
        _renderers    = renderers.ToDictionary(r => r.Format);
        _auditService = auditService;
        _logger       = logger;

        _logger.LogInformation(
            "ReportServiceV2 inicializado. Sources registrados: [{Sources}]. Renderers registrados: [{Renderers}].",
            string.Join(", ", _sources.Keys),
            string.Join(", ", _renderers.Keys));
    }

    /// <inheritdoc/>
    public Task<byte[]> GeneratePdfAsync(
        ReportType      reportType,
        ReportFilterDto filter,
        HttpContext      context)
        => GenerateAsync(reportType, ReportFormat.Pdf, filter, context);

    /// <inheritdoc/>
    public Task<byte[]> GenerateExcelAsync(
        ReportType      reportType,
        ReportFilterDto filter,
        HttpContext      context)
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

        // ── Validaciones previas (fuera del try para no auditar errores de configuración) ──

        // 1. Seleccionar el source
        if (!_sources.TryGetValue(reportType, out var source))
        {
            var available = string.Join(", ", _sources.Keys);
            throw new InvalidOperationException(
                $"No existe un IReportSource registrado para el tipo '{reportType}'. " +
                $"Sources disponibles: [{available}].");
        }

        // 2. Seleccionar el renderer
        if (!_renderers.TryGetValue(format, out var renderer))
        {
            var available = string.Join(", ", _renderers.Keys);
            throw new InvalidOperationException(
                $"No existe un IReportRenderer registrado para el formato '{format}'. " +
                $"Renderers disponibles: [{available}].");
        }

        // ── Ejecución con auditoría automática ────────────────────────────────
        var sw           = Stopwatch.StartNew();
        byte[]? bytes    = null;
        string? errorMsg = null;
        string? fileName = null;

        try
        {
            // 3. Construir la definición del reporte
            _logger.LogDebug(
                "ReportServiceV2: invocando source '{SourceType}' para construir la definición.",
                source.GetType().Name);

            var definition = await source.BuildAsync(filter, context);

            _logger.LogInformation(
                "ReportServiceV2: definición construida. Título='{Title}', Filas={Rows}, Columnas={Cols}.",
                definition.Title,
                definition.Rows?.Count ?? 0,
                definition.Columns?.Count ?? 0);

            // 4. Renderizar y devolver bytes
            _logger.LogDebug(
                "ReportServiceV2: invocando renderer '{RendererType}'.",
                renderer.GetType().Name);

            bytes    = await renderer.RenderAsync(definition);
            fileName = ReportFileNameBuilder.Build(definition.FilePrefix ?? reportType.ToString(), format);

            _logger.LogInformation(
                "ReportServiceV2: reporte generado. Formato={Format}, Tamaño={Size} bytes.",
                format, bytes.Length);

            return bytes;
        }
        catch (Exception ex)
        {
            errorMsg = ex.Message;
            _logger.LogError(ex,
                "ReportServiceV2: error generando reporte. ReportType={ReportType}, Format={Format}.",
                reportType, format);
            throw;
        }
        finally
        {
            sw.Stop();
            // La auditoría se guarda siempre (éxito o error) sin interrumpir el flujo principal
            await SaveAuditAsync(
                reportType  : reportType.ToString(),
                format      : format.ToString(),
                filter      : filter,
                context     : context,
                fileSizeBytes: bytes?.Length,
                elapsedMs   : (int)sw.ElapsedMilliseconds,
                success     : errorMsg is null,
                errorMessage: errorMsg,
                fileName    : fileName);
        }
    }

    // ── Método privado de auditoría ───────────────────────────────────────────

    /// <summary>
    /// Persiste el registro de auditoría del reporte generado.
    /// Los errores internos de auditoría se registran en el log pero nunca
    /// interrumpen el flujo principal (fire-and-forget seguro con await).
    /// </summary>
    private async Task SaveAuditAsync(
        string          reportType,
        string          format,
        ReportFilterDto filter,
        HttpContext      context,
        long?           fileSizeBytes,
        int             elapsedMs,
        bool            success,
        string?         errorMessage,
        string?         fileName)
    {
        try
        {
            var userIdStr = context.GetUserId();
            var userId    = Guid.TryParse(userIdStr, out var uid) ? uid : (Guid?)null;
            var userEmail = context.User.Identity?.Name ?? "anonymous";
            var clientIp  = context.Connection.RemoteIpAddress?.ToString();

            var audit = new CreateReportAuditDto
            {
                UserId           = userId,
                UserEmail        = userEmail,
                ReportType       = reportType,
                ReportFormat     = format,
                FiltersApplied   = JsonSerializer.Serialize(filter),
                FileSizeBytes    = fileSizeBytes,
                GenerationTimeMs = elapsedMs,
                ClientIp         = clientIp,
                Success          = success,
                ErrorMessage     = errorMessage,
                FileName         = fileName,
            };

            await _auditService.CreateAuditAsync(audit);

            _logger.LogDebug(
                "ReportServiceV2: auditoría guardada. ReportType={ReportType}, Format={Format}, " +
                "Success={Success}, ElapsedMs={ElapsedMs}.",
                reportType, format, success, elapsedMs);
        }
        catch (Exception ex)
        {
            // La auditoría nunca debe romper el flujo principal
            _logger.LogError(ex,
                "ReportServiceV2: error al guardar la auditoría. ReportType={ReportType}, Format={Format}.",
                reportType, format);
        }
    }
}

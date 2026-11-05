using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace WsUtaSystem.Infrastructure.ExternalServices.Common;

/// <summary>
/// Registra cada llamada saliente a un servicio externo: servicio, método, host, código de respuesta y duración.
/// No registra ruta, query ni cuerpo (llevan cédulas y tokens). Permite medir el consumo de cada servicio
/// filtrando el log por el texto "ExternalCall".
/// </summary>
public sealed class ExternalCallLoggingHandler : DelegatingHandler
{
    private readonly string _service;
    private readonly ILogger _logger;

    public ExternalCallLoggingHandler(string service, ILoggerFactory loggerFactory)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
            .CreateLogger("ExternalCall");
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var host = request.RequestUri?.Host ?? "(sin host)";
        var started = Stopwatch.GetTimestamp();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            _logger.LogInformation(
                "ExternalCall {Service} {Method} {Host} -> {Status} en {ElapsedMs} ms",
                _service, request.Method.Method, host, (int)response.StatusCode,
                (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return response;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(
                "ExternalCall {Service} {Method} {Host} -> falló ({Error}) en {ElapsedMs} ms",
                _service, request.Method.Method, host, ex.GetType().Name,
                (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
    }
}

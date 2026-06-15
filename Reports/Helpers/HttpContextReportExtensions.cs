using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace WsUtaSystem.Reports.Helpers;

/// <summary>
/// Extensiones de <see cref="HttpContext"/> para uso interno del módulo de reportes.
/// </summary>
internal static class HttpContextReportExtensions
{
    internal const string ReportUserKey = "__report_generated_by";

    /// <summary>
    /// Devuelve el nombre para mostrar en la cabecera "Generado por" del reporte.
    /// <para>
    /// Prioridad: nombre completo resuelto desde BD (guardado por <c>ReportServiceV2</c>)
    /// → email del claim → identificador del sujeto → "Anónimo".
    /// </para>
    /// </summary>
    public static string GetReportUserName(this HttpContext context)
    {
        if (context.Items.TryGetValue(ReportUserKey, out var cached) && cached is string { Length: > 0 } name)
            return name;

        return context.User.FindFirst(ClaimTypes.Email)?.Value
            ?? context.User.FindFirst("email")?.Value
            ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "Anónimo";
    }
}

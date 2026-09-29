using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace WsUtaSystem.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task Invoke(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex) { await HandleAsync(ctx, ex); }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        var problem = ToProblem(ex);
        problem.Extensions["traceId"] = ctx.TraceIdentifier;
        _logger.LogError(ex, "Error en {Method} {Path}. TraceId={TraceId}", ctx.Request?.Method, ctx.Request?.Path.Value, ctx.TraceIdentifier);
        ctx.Response.ContentType = "application/problem+json";
        ctx.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(problem);
    }

    private static ProblemDetails ToProblem(Exception ex)
    {
        if (ex is DbUpdateException dbex && dbex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            return new ProblemDetails { Title = "Registro duplicado", Detail = "Violación de índice único.", Status = StatusCodes.Status409Conflict };
        if (ex is DbUpdateConcurrencyException cex)
            return new ProblemDetails { Title = "Conflicto de concurrencia", Detail = cex.Message, Status = StatusCodes.Status409Conflict };
        if (ex is FluentValidation.ValidationException vex)
        {
            var errors = vex.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return new ValidationProblemDetails(errors) { Title = "Solicitud inválida", Status = StatusCodes.Status400BadRequest };
        }
        if (ex is DbUpdateException dbex2)
            return new ProblemDetails { Title = "No se pudo completar la operación", Detail = TranslateSqlDetail(dbex2.InnerException as SqlException), Status = StatusCodes.Status400BadRequest };
        // Rechazo de una regla de negocio (ej. "ya tiene otro turno", "no se puede reasignar
        // un turno cancelado") — es el tipo de excepción que se lanza en toda la aplicación
        // para este propósito. Sin este caso caía al genérico y salía como 500 en vez de un
        // mensaje claro (hallazgo real: reasignación de guardias 2026-09-07).
        if (ex is InvalidOperationException ioex)
            return new ProblemDetails { Title = "No se puede completar la operación", Detail = ioex.Message, Status = StatusCodes.Status409Conflict };
        return new ProblemDetails { Title = "Error inesperado", Detail = "Ocurrió un error inesperado en el servidor. Contacte a soporte técnico si el problema persiste.", Status = StatusCodes.Status500InternalServerError };
    }

    // Traduce errores de SQL Server a mensajes en español sin exponer nombres de tabla/columna
    // (hallazgo informe UTA-DITIC-PS-027-2026, observaciones 18/20/22/26: el detalle crudo de
    // SqlException llegaba tal cual al usuario final, ej. "String or binary data would be
    // truncated in table 'dbUtaSystem.HR.tbl_Audit', column 'UserName'").
    private static string TranslateSqlDetail(SqlException? sql) => sql?.Number switch
    {
        547 => "No se puede completar la operación: el registro está relacionado con otra información del sistema.",
        8152 or 2628 => "Uno de los valores ingresados es demasiado largo para el campo correspondiente.",
        _ => "No se pudo completar la operación. Contacte a soporte técnico si el problema persiste."
    };
}

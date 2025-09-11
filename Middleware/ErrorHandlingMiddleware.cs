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
            return new ProblemDetails { Title = "No se pudo completar la operación", Detail = dbex2.InnerException?.Message ?? dbex2.Message, Status = StatusCodes.Status400BadRequest };
        return new ProblemDetails { Title = "Error inesperado", Detail = ex.Message, Status = StatusCodes.Status500InternalServerError };
    }
}

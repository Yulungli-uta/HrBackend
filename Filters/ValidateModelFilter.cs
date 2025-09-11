using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WsUtaSystem.Filters;

public sealed class ValidateModelFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(kv => kv.Value?.Errors?.Count > 0)
                .ToDictionary(kv => kv.Key, kv => kv.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var pd = new ValidationProblemDetails(errors)
            { Title = "Solicitud inválida", Status = StatusCodes.Status400BadRequest };
            context.Result = new BadRequestObjectResult(pd);
        }
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}

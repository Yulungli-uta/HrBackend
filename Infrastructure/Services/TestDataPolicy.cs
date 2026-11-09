using Microsoft.Extensions.Options;
using WsUtaSystem.Application.Common.Options;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="ITestDataPolicy"/>: los datos de prueba se muestran solo cuando
/// <c>TestData:Visible = true</c> en la configuración (appsettings por entorno; false por defecto).
/// </summary>
/// <remarks>
/// Lee el empleado del request directamente de <see cref="IHttpContextAccessor"/> (claim "employeeId" o
/// <c>HttpContext.Items["EmployeeId"]</c>, la misma fuente que <c>CurrentUserService.EmployeeId</c>) y no de
/// <c>ICurrentUserService</c>: ese servicio depende de repositorios que a su vez usan <c>AppDbContext</c>, que
/// consume esta política, y generaría una dependencia circular. Tampoco consulta la base de datos.
/// </remarks>
public sealed class TestDataPolicy : ITestDataPolicy
{
    private readonly IHttpContextAccessor _http;

    public TestDataPolicy(IOptions<TestDataOptions> options, IHttpContextAccessor http)
    {
        ArgumentNullException.ThrowIfNull(options);
        _http = http ?? throw new ArgumentNullException(nameof(http));
        IncludeTestData = options.Value.Visible;
    }

    public bool IncludeTestData { get; }

    public int? CurrentEmployeeId
    {
        get
        {
            var ctx = _http.HttpContext;
            if (ctx?.User is null) return null;

            if (int.TryParse(ctx.User.FindFirst("employeeId")?.Value, out var claimId))
                return claimId;

            if (ctx.Items["EmployeeId"] is not null &&
                int.TryParse(ctx.Items["EmployeeId"]!.ToString(), out var itemId))
                return itemId;

            return null;
        }
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Infrastructure.Common;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    // ✅ Identidad (GUID)
    public string? Subject =>
        User?.FindFirst("sub")?.Value
        ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User?.FindFirst("userId")?.Value;

    // ✅ Negocio (INT) - SOLO employeeId
    public int? EmployeeId
    {
        get
        {
            var ctx = _http.HttpContext;
            var user = ctx?.User;
            if (user is null) return null;

            var empClaim = user.FindFirst("employeeId")?.Value;
            if (int.TryParse(empClaim, out var empId))
                return empId;

            if (ctx?.Items["EmployeeId"] is not null &&
                int.TryParse(ctx.Items["EmployeeId"]!.ToString(), out var itemEmpId))
                return itemEmpId;

            return null;
        }
    }

    public string? UserName
        => User?.FindFirst(ClaimTypes.Name)?.Value
           ?? User?.Identity?.Name;

    public string? Email
        => User?.FindFirst(ClaimTypes.Email)?.Value
           ?? User?.FindFirst("email")?.Value;

    public string? GetIp()
    {
        var ctx = _http.HttpContext;
        if (ctx == null) return null;

        var xff = ctx.Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(xff))
            return xff.Split(',')[0].Trim();

        var xReal = ctx.Request.Headers["X-Real-IP"].ToString();
        if (!string.IsNullOrWhiteSpace(xReal))
            return xReal.Trim();

        var ip = ctx.Connection.RemoteIpAddress?.ToString();
        return ip == "::1" ? "127.0.0.1" : ip;
    }

    public string? GetUserAgent()
    {
        var ctx = _http.HttpContext;
        if (ctx == null) return null;

        var ua = ctx.Request.Headers["User-Agent"].ToString();
        return string.IsNullOrWhiteSpace(ua) ? null : ua;
    }

    public string? GetDeviceInfo()
    {
        var ctx = _http.HttpContext;
        if (ctx == null) return null;

        var device = ctx.Request.Headers["X-Device-Info"].ToString();
        return string.IsNullOrWhiteSpace(device) ? null : device;
    }
}

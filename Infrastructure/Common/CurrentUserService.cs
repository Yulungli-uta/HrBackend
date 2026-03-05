using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Infrastructure.Common;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    private readonly IvwEmployeeDetailsService _employeeDetails;
    private readonly ILogger<CurrentUserService> _logger;
    
    private const string BossCacheKey = "__CurrentUser_BossInfo";
    private const string MeCacheKey = "__CurrentUser_EmployeeDetails";


    public CurrentUserService(IHttpContextAccessor http, IvwEmployeeDetailsService employeeDetails,
        ILogger<CurrentUserService> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _employeeDetails = employeeDetails ?? throw new ArgumentNullException(nameof(employeeDetails));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private HttpContext? Ctx => _http.HttpContext;
    private ClaimsPrincipal? User => Ctx?.User;

    //private ClaimsPrincipal? User => _http.HttpContext?.User;

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

    // Estas propiedades NO consultan BD. Solo leen caché si ya fue cargada por LoadBossAsync().
    public int? BossId => TryGetBossFromCache()?.BossId;
    public string? BossName => TryGetBossFromCache()?.FullName;
    public string? BossEmail => TryGetBossFromCache()?.Email;
    public int? DepartmentID => TryGetMeFromCache()?.DepartmentID;
    public string? DepartmentName => TryGetMeFromCache()?.Department;

    public async Task<CurrentBossInfo?> LoadBossAsync(CancellationToken ct = default)
    {
        var ctx = Ctx;
        if (ctx is null) return null;

        // 1) Si ya está en caché, devolver
        if (ctx.Items.TryGetValue(BossCacheKey, out var cached) && cached is CurrentBossInfo cachedBoss)
            return cachedBoss;

        // 2) Resolver mi empleado
        var myId = EmployeeId;
        if (myId is null || myId <= 0)
            return null;

        _logger.LogInformation("********filling boss informacion with employeid:{myId} ", myId);
        VwEmployeeDetails? me = await GetMeDetailsAsync(myId.Value, ct);
        _logger.LogInformation(
            "******** filling boss information with employeeId: {EmployeeId} | me: {MeJson}",
            myId,
            JsonSerializer.Serialize(me)
        );
        if (me is null)
            return null;

        var bossId = me.ImmediateBossID;
        if (bossId is null || bossId <= 0)
        {
            _logger.LogInformation("Employee {EmployeeId} has no ImmediateBossID.", myId);
            return null;
        }

        // 3) Resolver jefe
        var boss = await _employeeDetails.GetEmployeeDetailsAsync(bossId.Value, ct);
        if (boss is null)
        {
            _logger.LogWarning("Boss not found. BossId={BossId}, EmployeeId={EmployeeId}", bossId, myId);
            return null;
        }

        var bossInfo = new CurrentBossInfo(
            BossId: boss.EmployeeID,
            FullName: boss.FullName,
            Email: boss.Email
        );

        // 4) Guardar en caché por request
        ctx.Items[BossCacheKey] = bossInfo;

        return bossInfo;
    }

    public async Task<VwEmployeeDetails?> LoadMeAsync(CancellationToken ct = default)
    {
        var myId = EmployeeId;
        if (myId is null || myId <= 0) return null;

        return await GetMeDetailsAsync(myId.Value, ct);
    }

    private CurrentBossInfo? TryGetBossFromCache()
    {
        var ctx = Ctx;
        if (ctx is null) return null;
        return ctx.Items.TryGetValue(BossCacheKey, out var cached) ? cached as CurrentBossInfo : null;
    }

    private async Task<VwEmployeeDetails?> GetMeDetailsAsync(int employeeId, CancellationToken ct)
    {
        var ctx = Ctx;
        if (ctx is null) return null;

        if (ctx.Items.TryGetValue(MeCacheKey, out var cached) && cached is VwEmployeeDetails c)
            return c;

        var me = await _employeeDetails.GetEmployeeDetailsAsync(employeeId, ct);
        if (me is not null)
            ctx.Items[MeCacheKey] = me;

        return me;
    }

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

    private VwEmployeeDetails? TryGetMeFromCache()
    {
        var ctx = Ctx;
        if (ctx is null) return null;
        return ctx.Items.TryGetValue(MeCacheKey, out var cached)
            ? cached as VwEmployeeDetails
            : null;
    }
}

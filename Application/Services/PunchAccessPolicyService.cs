using System.Net;
using Microsoft.Extensions.Caching.Memory;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.PunchPolicy;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class PunchAccessPolicyService : IPunchAccessPolicyService
{
    public const string ModeOff = "OFF";
    public const string ModeAudit = "AUDIT";
    public const string ModeEnforce = "ENFORCE";

    private const string CacheModeKey = "punchnet:mode";
    private const string CacheNetworksKey = "punchnet:networks";
    private const string CacheMinIntervalKey = "punchnet:minInterval";
    private const string MinIntervalParameter = "PUNCH_MIN_INTERVAL_MINUTES";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);
    private const int MaxPageSize = 100;

    private const string DeniedMessage =
        "Solo puedes registrar tu picada dentro del campus. Si necesitas picar desde otro lugar, solicita la autorización a Talento Humano.";

    private readonly IPunchPolicyRepository _repo;
    private readonly IMemoryCache _cache;
    private readonly IHostEnvironment _env;
    private readonly ILogger<PunchAccessPolicyService> _logger;

    private readonly IAppClock _clock;

    public PunchAccessPolicyService(
        IPunchPolicyRepository repo, IMemoryCache cache, IHostEnvironment env, ILogger<PunchAccessPolicyService> logger,
        IAppClock clock)
    {
        _clock = clock;
        _repo = repo;
        _cache = cache;
        _env = env;
        _logger = logger;
    }

    // ── Evaluación de una picada ────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<PunchAccessDecision> EvaluateAsync(int employeeId, string? clientIp, string? forwardedFor, CancellationToken ct)
    {
        var mode = await GetModeCachedAsync(ct);
        if (mode == ModeOff) return new PunchAccessDecision(true, "UNCHECKED", mode, null);

        var networks = await GetNetworksCachedAsync(ct);

        // Sin ninguna red activa no hay forma de saber qué es "campus": en AUDIT se registraría CADA picada de todos
        // los empleados como "fuera del campus". Hasta configurar redes no se evalúa ni se registra nada.
        // (ENFORCE no puede estar activo sin redes: SetModeAsync y la edición de redes lo impiden.)
        if (networks.Count == 0 && mode != ModeEnforce)
            return new PunchAccessDecision(true, "UNCHECKED", mode, null);

        if (IsCampus(clientIp, networks))
            return new PunchAccessDecision(true, "CAMPUS", mode, null);

        // Fuera del campus: solo pasa con autorización vigente del empleado.
        var policy = await _repo.GetEffectivePolicyAsync(employeeId, _clock.Today.ToDateTime(TimeOnly.MinValue), ct);
        var authorized = policy is not null;

        if (authorized)
        {
            await TryLogAsync(employeeId, clientIp, forwardedFor, "EXTERNAL_ALLOWED", mode, policy!.Reason, ct);
            return new PunchAccessDecision(true, "EXTERNAL", mode, null);
        }

        if (mode == ModeEnforce)
        {
            await TryLogAsync(employeeId, clientIp, forwardedFor, "REJECTED", mode, "Fuera del campus sin autorización", ct);
            return new PunchAccessDecision(false, "BLOCKED", mode, DeniedMessage);
        }

        await TryLogAsync(employeeId, clientIp, forwardedFor, "AUDIT_WOULD_REJECT", mode, "Fuera del campus sin autorización (modo registro)", ct);
        return new PunchAccessDecision(true, "EXTERNAL_UNAUTHORIZED", mode, null);
    }

    /// <inheritdoc />
    public async Task<PunchAccessInfoDto> GetMyAccessAsync(int employeeId, string? clientIp, CancellationToken ct)
    {
        var mode = await GetModeCachedAsync(ct);
        var policy = await _repo.GetEffectivePolicyAsync(employeeId, _clock.Today.ToDateTime(TimeOnly.MinValue), ct);
        return new PunchAccessInfoDto
        {
            Ip = clientIp,
            Mode = mode,
            IsCampus = IsCampus(clientIp, await GetNetworksCachedAsync(ct)),
            HasExternalPermission = policy is not null
        };
    }

    private bool IsCampus(string? clientIp, IReadOnlyList<System.Net.IPNetwork> networks)
    {
        if (!IPAddress.TryParse(clientIp, out var ip)) return false;
        // En desarrollo local la IP es la del propio equipo; en servidores nunca se acepta loopback como campus.
        if (_env.IsDevelopment() && IPAddress.IsLoopback(IpNetworkMatcher.Normalize(ip))) return true;
        return IpNetworkMatcher.Contains(networks, ip);
    }

    /// <inheritdoc />
    public Task LogEventAsync(int employeeId, string? clientIp, string? forwardedFor, string result, string mode, string? detail, CancellationToken ct) =>
        TryLogAsync(employeeId, clientIp, forwardedFor, Truncate(result, 30)!, Truncate(mode, 10)!, detail, ct);

    // Registrar nunca debe impedir la picada: un fallo de bitácora solo se anota en el log técnico.
    private async Task TryLogAsync(int employeeId, string? ip, string? forwardedFor, string result, string mode, string? detail, CancellationToken ct)
    {
        try
        {
            await _repo.AddLogAsync(new PunchAccessLog
            {
                EmployeeId = employeeId,
                AttemptedAt = _clock.Now,
                IpAddress = Truncate(ip, 64),
                ForwardedFor = Truncate(forwardedFor, 200),
                Result = result,
                Mode = mode,
                Detail = Truncate(detail, 200)
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar el acceso de picada del empleado {EmployeeId}", employeeId);
        }
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : (value.Length <= max ? value : value[..max]);

    // ── Redes del campus ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<PunchNetworkDto>> GetNetworksAsync(CancellationToken ct) =>
        (await _repo.GetNetworksAsync(ct)).Select(ToDto).ToList();

    public async Task<PunchNetworkDto> CreateNetworkAsync(PunchNetworkSaveDto dto, int? userEmployeeId, CancellationToken ct)
    {
        var normalized = ValidateNetwork(dto);
        if (await _repo.NetworkExistsAsync(normalized, null, ct))
            throw new BusinessRuleException("Esa IP o rango ya está en la lista.");

        var created = await _repo.AddNetworkAsync(new AllowedPunchNetwork
        {
            Name = dto.Name.Trim(),
            IpOrCidr = normalized,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            IsActive = dto.IsActive,
            CreatedAt = _clock.Now,
            CreatedBy = userEmployeeId
        }, ct);
        InvalidateNetworks();
        return ToDto(created);
    }

    public async Task<PunchNetworkDto?> UpdateNetworkAsync(int networkId, PunchNetworkSaveDto dto, int? userEmployeeId, CancellationToken ct)
    {
        var entity = await _repo.GetNetworkAsync(networkId, ct);
        if (entity is null) return null;

        var normalized = ValidateNetwork(dto);
        if (await _repo.NetworkExistsAsync(normalized, networkId, ct))
            throw new BusinessRuleException("Esa IP o rango ya está en la lista.");

        // En modo ENFORCE no se puede dejar la lista sin ninguna red activa: nadie podría picar.
        if (!dto.IsActive && entity.IsActive && await GetModeCachedAsync(ct) == ModeEnforce
            && (await _repo.GetNetworksAsync(ct)).Count(n => n.IsActive && n.NetworkId != networkId) == 0)
            throw new BusinessRuleException("No se puede desactivar la última red mientras el control está en modo bloqueo.");

        entity.Name = dto.Name.Trim();
        entity.IpOrCidr = normalized;
        entity.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = _clock.Now;
        entity.UpdatedBy = userEmployeeId;
        await _repo.UpdateNetworkAsync(entity, ct);
        InvalidateNetworks();
        return ToDto(entity);
    }

    public async Task<bool> DeleteNetworkAsync(int networkId, CancellationToken ct)
    {
        var entity = await _repo.GetNetworkAsync(networkId, ct);
        if (entity is null) return false;

        if (entity.IsActive && await GetModeCachedAsync(ct) == ModeEnforce
            && (await _repo.GetNetworksAsync(ct)).Count(n => n.IsActive && n.NetworkId != networkId) == 0)
            throw new BusinessRuleException("No se puede eliminar la última red mientras el control está en modo bloqueo.");

        var deleted = await _repo.DeleteNetworkAsync(networkId, ct);
        InvalidateNetworks();
        return deleted;
    }

    public async Task<PunchIpCheckDto> CheckIpAsync(string ip, CancellationToken ct)
    {
        var result = new PunchIpCheckDto { Ip = (ip ?? string.Empty).Trim() };
        if (!IPAddress.TryParse(result.Ip, out var parsed))
        {
            result.Message = "La IP no es válida.";
            return result;
        }
        result.IsValid = true;
        foreach (var n in (await _repo.GetNetworksAsync(ct)).Where(n => n.IsActive))
        {
            if (IpNetworkMatcher.TryParseEntry(n.IpOrCidr, out var network, out _, out _)
                && IpNetworkMatcher.Contains(new[] { network }, parsed))
            {
                result.IsCampus = true;
                result.MatchedNetwork = $"{n.Name} ({n.IpOrCidr})";
                break;
            }
        }
        result.Message = result.IsCampus ? "La IP pertenece al campus." : "La IP no está en ninguna red activa del campus.";
        return result;
    }

    private static string ValidateNetwork(PunchNetworkSaveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Ingrese un nombre para la red.");
        if (!IpNetworkMatcher.TryParseEntry(dto.IpOrCidr, out _, out var normalized, out var error))
            throw new ArgumentException(error);
        return normalized;
    }

    private static PunchNetworkDto ToDto(AllowedPunchNetwork n) => new()
    {
        NetworkId = n.NetworkId,
        Name = n.Name,
        IpOrCidr = n.IpOrCidr,
        Notes = n.Notes,
        IsActive = n.IsActive,
        CreatedAt = n.CreatedAt,
        UpdatedAt = n.UpdatedAt
    };

    // ── Autorizaciones de picada externa (historial) ────────────────────────

    public async Task<PagedResult<PunchPolicyItemDto>> GetPoliciesAsync(
        string? search, bool onlyEffective, int? employeeId, Guid? batchId, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await _repo.GetPoliciesPagedAsync(search, onlyEffective, employeeId, batchId, TodayDate(), page, pageSize, ct);
        return new PagedResult<PunchPolicyItemDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<PunchPolicyItemDto?> CreatePolicyAsync(PunchPolicyCreateDto dto, int? authorizedBy, CancellationToken ct)
    {
        if (!await _repo.EmployeeExistsAsync(dto.EmployeeId, ct)) return null;
        var (from, to, reason) = ValidatePolicyInput(dto.ValidFrom, dto.ValidTo, dto.Reason);

        var overlap = await _repo.GetEmployeesWithOverlapAsync(new[] { dto.EmployeeId }, from, to, null, ct);
        if (overlap.Count > 0)
            throw new BusinessRuleException("El empleado ya tiene una autorización vigente que se cruza con esas fechas. Revóquela primero o ajuste las fechas.");

        var policy = NewPolicy(dto.EmployeeId, reason, from, to, authorizedBy, null, null);
        await _repo.AddPoliciesAsync(new[] { policy }, ct);
        return await _repo.GetPolicyItemAsync(policy.PolicyId, TodayDate(), ct);
    }

    public async Task<PunchPolicyItemDto> CreateGlobalPolicyAsync(PunchPolicyGlobalDto dto, int? authorizedBy, CancellationToken ct)
    {
        var (from, to, reason) = ValidatePolicyInput(dto.ValidFrom, dto.ValidTo, dto.Reason);
        // Autorizar a toda la institución deja sin efecto la restricción de red: debe tener fecha de fin.
        if (to is null) throw new ArgumentException("La autorización a toda la institución debe tener fecha final.");
        if (await _repo.HasGlobalOverlapAsync(from, to, ct))
            throw new BusinessRuleException("Ya existe una autorización a toda la institución que se cruza con esas fechas. Revóquela primero.");

        var policy = NewPolicy(null, reason, from, to, authorizedBy, null, null);
        policy.IsGlobal = true;
        await _repo.AddPoliciesAsync(new[] { policy }, ct);
        _logger.LogWarning("Autorización de picada externa GLOBAL creada (policy {PolicyId}) hasta {To:yyyy-MM-dd}", policy.PolicyId, to);
        return (await _repo.GetPolicyItemAsync(policy.PolicyId, TodayDate(), ct))!;
    }

    public async Task<PunchPolicyDepartmentPreviewDto> PreviewDepartmentPoliciesAsync(
        int departmentId, bool includeChildren, DateTime? validFrom, DateTime? validTo, CancellationToken ct)
    {
        var name = await _repo.GetDepartmentNameAsync(departmentId, ct) ?? throw new ArgumentException("El departamento no existe o está inactivo.");
        var (from, to, _) = ValidatePolicyInput(validFrom, validTo, null);
        var (employees, departments) = await _repo.GetEmployeesOfDepartmentAsync(departmentId, includeChildren, ct);
        var overlap = await _repo.GetEmployeesWithOverlapAsync(employees, from, to, null, ct);
        return new PunchPolicyDepartmentPreviewDto
        {
            DepartmentName = name, Departments = departments, Employees = employees.Count,
            AlreadyAuthorized = overlap.Count, ToCreate = employees.Count - overlap.Count
        };
    }

    public async Task<PunchPolicyBatchResultDto> CreateDepartmentPoliciesAsync(PunchPolicyDepartmentDto dto, int? authorizedBy, CancellationToken ct)
    {
        _ = await _repo.GetDepartmentNameAsync(dto.DepartmentId, ct) ?? throw new ArgumentException("El departamento no existe o está inactivo.");
        var (from, to, reason) = ValidatePolicyInput(dto.ValidFrom, dto.ValidTo, dto.Reason);
        var (employees, _) = await _repo.GetEmployeesOfDepartmentAsync(dto.DepartmentId, dto.IncludeChildren, ct);
        if (employees.Count == 0) throw new BusinessRuleException("El departamento no tiene personal activo asignado.");

        // Quien ya tiene una autorización vigente que se cruza se omite (no se duplica ni se pisa).
        var overlap = await _repo.GetEmployeesWithOverlapAsync(employees, from, to, null, ct);
        var batchId = Guid.NewGuid();
        var policies = employees.Where(id => !overlap.Contains(id))
            .Select(id => NewPolicy(id, reason, from, to, authorizedBy, batchId, dto.DepartmentId)).ToList();
        if (policies.Count == 0) throw new BusinessRuleException("Todo el personal del departamento ya tiene una autorización vigente en esas fechas.");

        await _repo.AddPoliciesAsync(policies, ct);
        _logger.LogWarning("Autorización de picada externa por departamento {DepartmentId}: {Created} creadas, {Skipped} omitidas (lote {BatchId})",
            dto.DepartmentId, policies.Count, overlap.Count, batchId);
        return new PunchPolicyBatchResultDto { BatchId = batchId, Created = policies.Count, Skipped = overlap.Count };
    }

    public async Task<PunchPolicyItemDto?> RevokePolicyAsync(int policyId, PunchPolicyRevokeDto dto, int? revokedBy, CancellationToken ct)
    {
        var policy = await _repo.GetPolicyAsync(policyId, ct);
        if (policy is null) return null;
        if (policy.RevokedAt is null)
        {
            MarkRevoked(policy, dto.Reason, revokedBy);
            await _repo.SavePoliciesAsync(ct);
        }
        return await _repo.GetPolicyItemAsync(policyId, TodayDate(), ct);
    }

    public async Task<int> RevokeBatchAsync(Guid batchId, PunchPolicyRevokeDto dto, int? revokedBy, CancellationToken ct)
    {
        var open = await _repo.GetOpenPoliciesOfBatchAsync(batchId, ct);
        foreach (var p in open) MarkRevoked(p, dto.Reason, revokedBy);
        if (open.Count > 0) await _repo.SavePoliciesAsync(ct);
        return open.Count;
    }

    private void MarkRevoked(EmployeePunchPolicy policy, string? reason, int? revokedBy)
    {
        policy.AllowExternalPunch = false;   // compatibilidad: el código anterior también ve la fila como desactivada
        policy.RevokedAt = _clock.Now;
        policy.RevokedBy = revokedBy;
        policy.RevokeReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        policy.UpdatedAt = _clock.Now;
        policy.UpdatedBy = revokedBy;
    }

    private EmployeePunchPolicy NewPolicy(int? employeeId, string reason, DateTime? from, DateTime? to, int? authorizedBy, Guid? batchId, int? departmentId) => new()
    {
        EmployeeId = employeeId,
        AllowExternalPunch = true,
        Reason = reason,
        ValidFrom = from,
        ValidTo = to,
        AuthorizedBy = authorizedBy,
        CreatedAt = _clock.Now,
        BatchId = batchId,
        BatchDepartmentId = departmentId
    };

    private DateTime TodayDate() => _clock.Today.ToDateTime(TimeOnly.MinValue);

    private static (DateTime? From, DateTime? To, string Reason) ValidatePolicyInput(DateTime? from, DateTime? to, string? reason)
    {
        if (from.HasValue && to.HasValue && to.Value.Date < from.Value.Date)
            throw new ArgumentException("La fecha final no puede ser anterior a la inicial.");
        if (reason is not null && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Indique el motivo de la autorización (por ejemplo, teletrabajo autorizado).");
        return (from?.Date, to?.Date, reason?.Trim() ?? string.Empty);
    }

    // ── Registro ────────────────────────────────────────────────────────────

    public async Task<PagedResult<PunchAccessLogItemDto>> GetLogAsync(
        DateTime? from, DateTime? to, int? employeeId, string? result, int page, int pageSize, CancellationToken ct)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        var (items, total) = await _repo.GetLogPagedAsync(from, to, employeeId, string.IsNullOrWhiteSpace(result) ? null : result, page, pageSize, ct);
        return new PagedResult<PunchAccessLogItemDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, MaxPageSize));

    // ── Modo de operación ───────────────────────────────────────────────────

    public async Task<PunchSettingsDto> GetSettingsAsync(CancellationToken ct) => new()
    {
        Mode = await GetModeCachedAsync(ct),
        ActiveNetworks = (await _repo.GetNetworksAsync(ct)).Count(n => n.IsActive)
    };

    public async Task<PunchSettingsDto> SetModeAsync(string mode, CancellationToken ct)
    {
        var value = (mode ?? string.Empty).Trim().ToUpperInvariant();
        if (value is not (ModeOff or ModeAudit or ModeEnforce))
            throw new ArgumentException("El modo debe ser OFF, AUDIT o ENFORCE.");

        if (value == ModeEnforce && !(await _repo.GetNetworksAsync(ct)).Any(n => n.IsActive))
            throw new BusinessRuleException("Agregue al menos una red activa del campus antes de activar el bloqueo: de lo contrario nadie podría picar.");

        await _repo.SetModeAsync(value, ct);
        _cache.Remove(CacheModeKey);
        _logger.LogWarning("Modo de control de picadas cambiado a {Mode}", value);
        return await GetSettingsAsync(ct);
    }

    /// <inheritdoc />
    public async Task<int> GetMinIntervalMinutesAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheMinIntervalKey, out int cached)) return cached;
        var minutes = PunchCooldown.ParseMinutes(await _repo.GetParameterAsync(MinIntervalParameter, ct));
        _cache.Set(CacheMinIntervalKey, minutes, CacheTtl);
        return minutes;
    }

    // ── Caché de configuración (1 minuto; se invalida al editar) ────────────

    private async Task<string> GetModeCachedAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheModeKey, out string? cached) && cached is not null) return cached;
        var raw = (await _repo.GetModeAsync(ct))?.Trim().ToUpperInvariant();
        // Sin parámetro o con un valor desconocido no se bloquea a nadie.
        var mode = raw is ModeOff or ModeAudit or ModeEnforce ? raw : ModeOff;
        _cache.Set(CacheModeKey, mode, CacheTtl);
        return mode;
    }

    private async Task<IReadOnlyList<System.Net.IPNetwork>> GetNetworksCachedAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheNetworksKey, out IReadOnlyList<System.Net.IPNetwork>? cached) && cached is not null) return cached;
        var list = new List<System.Net.IPNetwork>();
        foreach (var n in (await _repo.GetNetworksAsync(ct)).Where(n => n.IsActive))
        {
            if (IpNetworkMatcher.TryParseEntry(n.IpOrCidr, out var network, out _, out _)) list.Add(network);
            else _logger.LogWarning("La red de picadas {NetworkId} tiene un valor inválido y se ignora: {Value}", n.NetworkId, n.IpOrCidr);
        }
        _cache.Set(CacheNetworksKey, (IReadOnlyList<System.Net.IPNetwork>)list, CacheTtl);
        return list;
    }

    private void InvalidateNetworks() => _cache.Remove(CacheNetworksKey);
}

using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Application.DTOs.PunchPolicy;

/// <summary>IP o rango del campus desde el que se permite picar.</summary>
public sealed class PunchNetworkDto
{
    public int NetworkId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpOrCidr { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class PunchNetworkSaveDto
{
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(64)] public string IpOrCidr { get; set; } = string.Empty;
    [StringLength(300)] public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Resultado de probar una IP contra la lista del campus.</summary>
public sealed class PunchIpCheckDto
{
    public string Ip { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsCampus { get; set; }
    public string? MatchedNetwork { get; set; }
    public string? Message { get; set; }
}

/// <summary>Autorización de picada externa de un empleado (con datos para mostrar en la pantalla).</summary>
public sealed class PunchPolicyItemDto
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? IdCard { get; set; }
    public string? Department { get; set; }
    public bool AllowExternalPunch { get; set; }
    public string? Reason { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    /// <summary>true si hoy está autorizado (activo y dentro de la vigencia).</summary>
    public bool IsEffective { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class PunchPolicySaveDto
{
    public bool AllowExternalPunch { get; set; }
    [StringLength(300)] public string? Reason { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public sealed class PunchAccessLogItemDto
{
    public long LogId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime AttemptedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? ForwardedFor { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string? Detail { get; set; }
}

/// <summary>Modo de operación del control: OFF, AUDIT (solo registra) o ENFORCE (bloquea).</summary>
public sealed class PunchSettingsDto
{
    [Required] public string Mode { get; set; } = "AUDIT";
    public int ActiveNetworks { get; set; }
}

/// <summary>Lo que el servidor ve de quien consulta: IP de origen y si puede picar desde ahí.</summary>
public sealed class PunchAccessInfoDto
{
    public string? Ip { get; set; }
    public bool IsCampus { get; set; }
    public bool HasExternalPermission { get; set; }
    public string Mode { get; set; } = "AUDIT";
}

/// <summary>Decisión de acceso para una picada.</summary>
public sealed record PunchAccessDecision(bool Allowed, string Origin, string Mode, string? Message);

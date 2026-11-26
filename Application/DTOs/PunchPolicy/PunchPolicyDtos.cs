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

/// <summary>Autorización de picada externa de un empleado (una fila del historial).</summary>
public sealed class PunchPolicyItemDto
{
    public int PolicyId { get; set; }
    /// <summary>Null en la autorización global.</summary>
    public int? EmployeeId { get; set; }
    /// <summary>true = autoriza a toda la institución (una sola fila).</summary>
    public bool IsGlobal { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? IdCard { get; set; }
    public string? Department { get; set; }
    public string? Reason { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    /// <summary>ACTIVE | SCHEDULED (aún no inicia) | EXPIRED (venció) | REVOKED.</summary>
    public string Status { get; set; } = "ACTIVE";
    /// <summary>true si hoy permite picar fuera del campus (Status = ACTIVE).</summary>
    public bool IsEffective { get; set; }
    public string? AuthorizedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByName { get; set; }
    public string? RevokeReason { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchDepartmentName { get; set; }
}

/// <summary>Nueva autorización individual. Las autorizaciones no se editan: se revocan y se crea otra.</summary>
public sealed class PunchPolicyCreateDto
{
    [Range(1, int.MaxValue)] public int EmployeeId { get; set; }
    [Required, StringLength(300)] public string Reason { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

/// <summary>Autorización global: una sola fila que permite picar fuera del campus a TODO el personal. La fecha final es obligatoria.</summary>
public sealed class PunchPolicyGlobalDto
{
    [Required, StringLength(300)] public string Reason { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    [Required] public DateTime? ValidTo { get; set; }
}

/// <summary>Autorización para todo el personal activo de un departamento (una fila por empleado, mismo lote).</summary>
public sealed class PunchPolicyDepartmentDto
{
    [Range(1, int.MaxValue)] public int DepartmentId { get; set; }
    public bool IncludeChildren { get; set; }
    [Required, StringLength(300)] public string Reason { get; set; } = string.Empty;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

/// <summary>Vista previa de una autorización por departamento (no escribe nada).</summary>
public sealed class PunchPolicyDepartmentPreviewDto
{
    public string DepartmentName { get; set; } = string.Empty;
    public int Departments { get; set; }
    public int Employees { get; set; }
    /// <summary>Se omiten los que ya tienen otra autorización vigente que se cruza con las fechas.</summary>
    public int AlreadyAuthorized { get; set; }
    public int ToCreate { get; set; }
}

public sealed class PunchPolicyBatchResultDto
{
    public Guid BatchId { get; set; }
    public int Created { get; set; }
    public int Skipped { get; set; }
}

public sealed class PunchPolicyRevokeDto
{
    [StringLength(300)] public string? Reason { get; set; }
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

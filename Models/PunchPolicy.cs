namespace WsUtaSystem.Models
{
    /// <summary>IP o rango CIDR del campus desde el cual se permite picar (HR.tbl_AllowedPunchNetworks).</summary>
    public class AllowedPunchNetwork
    {
        public int NetworkId { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>IPv4/IPv6 o rango CIDR normalizado (ej. "190.10.20.0/24").</summary>
        public string IpOrCidr { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }

    /// <summary>Autorización de un empleado para picar fuera del campus (HR.tbl_EmployeePunchPolicies).</summary>
    public class EmployeePunchPolicy
    {
        public int PolicyId { get; set; }
        public int EmployeeId { get; set; }
        public bool AllowExternalPunch { get; set; }
        public string? Reason { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int? AuthorizedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }

    /// <summary>Picada fuera del campus: permitida, rechazada o que se habría rechazado (HR.tbl_PunchAccessLog).</summary>
    public class PunchAccessLog
    {
        public long LogId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime AttemptedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? ForwardedFor { get; set; }
        /// <summary>EXTERNAL_ALLOWED | REJECTED | AUDIT_WOULD_REJECT.</summary>
        public string Result { get; set; } = string.Empty;
        /// <summary>AUDIT | ENFORCE.</summary>
        public string Mode { get; set; } = string.Empty;
        public string? Detail { get; set; }
    }
}

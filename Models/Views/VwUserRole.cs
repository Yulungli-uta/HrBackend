using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Views;

/// <summary>
/// Entidad que mapea la vista vw_UserRoles de SQL Server
/// Contiene información completa de usuarios y sus roles asignados
/// </summary>
[Table("vw_UserRoles", Schema = "dbo")]
public class VwUserRole
{
    /// <summary>
    /// ID del usuario
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email del usuario
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Nombre para mostrar del usuario
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de usuario (Local, AzureAD, etc.)
    /// </summary>
    public string UserType { get; set; } = string.Empty;

    /// <summary>
    /// ID del rol
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Nombre del rol
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del rol
    /// </summary>
    public string? RoleDescription { get; set; }

    /// <summary>
    /// Fecha y hora en que se asignó el rol al usuario
    /// </summary>
    public DateTime? AssignedAt { get; set; }

    /// <summary>
    /// Fecha y hora de expiración del rol (null si no expira)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Usuario que asignó el rol
    /// </summary>
    public string? AssignedBy { get; set; }
}

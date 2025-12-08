using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Views;

/// <summary>
/// Entidad que mapea la vista vw_RoleMenuItems de SQL Server
/// Contiene información completa de items de menú asignados a roles
/// </summary>
[Table("vw_RoleMenuItems", Schema = "dbo")]
public class VwRoleMenuItem
{
    /// <summary>
    /// ID del rol
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Nombre del rol
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// ID del item de menú
    /// </summary>
    public int MenuItemId { get; set; }

    /// <summary>
    /// Nombre del item de menú
    /// </summary>
    public string MenuItemName { get; set; } = string.Empty;

    /// <summary>
    /// URL del item de menú (ruta de navegación)
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Icono del item de menú
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// ID del item de menú padre (null si es raíz)
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// Orden de visualización del item
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Indica si el item es visible en el menú
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Visibilidad específica del rol para este item
    /// </summary>
    public bool RoleSpecificVisibility { get; set; }
}

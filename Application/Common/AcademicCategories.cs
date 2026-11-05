namespace WsUtaSystem.Application.Common;

/// <summary>
/// Nombres estables de las categorías académicas (<c>ref_Types.Category = 'ACADEMIC_CATEGORY'</c>).
/// Se resuelven por NOMBRE y no por Id porque <c>TypeId</c> es IDENTITY y varía entre ambientes.
/// </summary>
public static class AcademicCategories
{
    public const string TitularPrincipal = "TITULAR_PRINCIPAL";
    public const string TitularAgregado  = "TITULAR_AGREGADO";
    public const string TitularAuxiliar  = "TITULAR_AUXILIAR";

    /// <summary>Nombres de las categorías que se consideran titulares.</summary>
    public static readonly IReadOnlyCollection<string> TitularNames =
        [TitularPrincipal, TitularAgregado, TitularAuxiliar];

    /// <summary>Indica si el nombre de categoría corresponde a un docente titular.</summary>
    public static bool IsTitular(string? categoryName) =>
        categoryName is not null && TitularNames.Contains(categoryName, StringComparer.OrdinalIgnoreCase);
}

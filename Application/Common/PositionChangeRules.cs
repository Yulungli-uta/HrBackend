namespace WsUtaSystem.Application.Common;

/// <summary>
/// Reglas compartidas para decidir si una acción de personal cambia el puesto (dependencia o cargo) de la persona.
/// Licencias, cambios de dedicación, vacaciones, acciones disciplinarias y salidas no tienen situación propuesta en el
/// documento; en los datos algunas traen un departamento destino de relleno, que no debe tratarse como un movimiento.
/// </summary>
public static class PositionChangeRules
{
    /// <summary>Categorías de tipo de acción (<c>ActionCategory</c>) que nunca cambian el puesto.</summary>
    public static readonly string[] NonPositionCategories = ["LEAVE", "SCHEDULE", "VACATION", "DISCIPLINARY", "EXIT"];

    /// <summary>Código del tipo de acción "Designación" (<c>ref_personnel_action_type.Code</c>).</summary>
    public const string DesignationTypeCode = "DESIGNACION";

    /// <summary>
    /// Una Designación (p. ej. Coordinador de Carrera) de una persona que ya tiene un contrato activo del mismo régimen es una
    /// función que se suma al contrato, no una relación laboral nueva: no se registra como documento del régimen.
    /// </summary>
    public static bool IsConcurrentDesignation(string? actionTypeCode, bool hasActiveContractRegime) =>
        hasActiveContractRegime && string.Equals(actionTypeCode, DesignationTypeCode, StringComparison.OrdinalIgnoreCase);

    /// <summary>false si la categoría pertenece a un tipo de acción que no cambia el puesto. Sin categoría se considera que sí puede.</summary>
    public static bool CanChangePosition(string? actionCategory) =>
        actionCategory is null || !NonPositionCategories.Contains(actionCategory, StringComparer.OrdinalIgnoreCase);

    /// <summary>Categorías que por defecto imprimen la grilla completa de Situación Propuesta en el documento.</summary>
    public static readonly string[] ProposedSituationCategories = ["ENTRY", "MOVEMENT", "SALARY_CHANGE"];

    /// <summary>Valor inicial de "mostrar situación propuesta" de un tipo nuevo, según su categoría.</summary>
    public static bool ShowsProposedSituationByDefault(string? actionCategory) =>
        actionCategory is not null && ProposedSituationCategories.Contains(actionCategory, StringComparer.OrdinalIgnoreCase);

    /// <summary>true si la acción trae situación propuesta (departamento o cargo de destino).</summary>
    public static bool HasProposedSituation(int? destinationDepartmentId, int? destinationJobId) =>
        destinationDepartmentId.HasValue || destinationJobId.HasValue;
}

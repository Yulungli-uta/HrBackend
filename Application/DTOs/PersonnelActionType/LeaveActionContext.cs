namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

/// <summary>
/// Información del tipo de acción y del motivo de licencia necesaria para validar una acción de personal
/// (ver <c>PersonnelActionService</c>): si el tipo es un grupo con hijos, si pertenece a una serie que exige
/// motivo y si el motivo recibido es válido para esa serie.
/// </summary>
/// <param name="TypeExists">El tipo de acción existe.</param>
/// <param name="TypeIsActive">El tipo está activo.</param>
/// <param name="TypeHasChildren">El tipo es un grupo con hijos ACTIVOS (ej. "Licencia"): las acciones nuevas deben usar un hijo.</param>
/// <param name="ParentTypeId">Tipo padre; <c>null</c> si es raíz.</param>
/// <param name="TypeCode">Código del tipo.</param>
/// <param name="ReasonsAvailable">La serie del tipo tiene motivos activos en el catálogo (la acción debe llevar motivo).</param>
/// <param name="ReasonProvided">Se recibió un motivo.</param>
/// <param name="ReasonValid">El motivo recibido existe, está activo y aplica a la serie del tipo.</param>
public sealed record LeaveActionContext(
    bool TypeExists,
    bool TypeIsActive,
    bool TypeHasChildren,
    int? ParentTypeId,
    string? TypeCode,
    bool ReasonsAvailable,
    bool ReasonProvided,
    bool ReasonValid)
{
    /// <summary>Contexto de un tipo inexistente.</summary>
    public static LeaveActionContext NotFound { get; } =
        new(false, false, false, null, null, false, false, false);
}

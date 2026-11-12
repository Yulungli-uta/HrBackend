using WsUtaSystem.Models;

namespace WsUtaSystem.Application.DTOs.PersonnelActionType;

/// <summary>
/// Tipo general con sus subtipos (series). Es el elemento del listado jerárquico paginado por tipo general:
/// con búsqueda, <see cref="Children"/> trae solo los subtipos que coinciden (o todos si coincide el propio tipo general).
/// </summary>
/// <param name="Root">Tipo general (sin padre).</param>
/// <param name="Children">Subtipos visibles según la búsqueda.</param>
/// <param name="TotalChildren">Cantidad total de subtipos del tipo general, coincidan o no con la búsqueda.</param>
public sealed record PersonnelActionTypeFamily(
    Models.PersonnelActionType Root,
    IReadOnlyList<Models.PersonnelActionType> Children,
    int TotalChildren);

/// <summary>Respuesta del listado jerárquico: la misma información con DTOs de tipo.</summary>
public sealed record PersonnelActionTypeFamilyDto(
    PersonnelActionTypeDto Root,
    List<PersonnelActionTypeDto> Children,
    int TotalChildren);

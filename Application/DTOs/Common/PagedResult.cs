namespace WsUtaSystem.Application.DTOs.Common;

/// <summary>
/// Resultado genérico paginado para cualquier entidad del sistema.
/// Encapsula los datos de la página actual junto con la metadata de paginación
/// necesaria para que el cliente construya controles de navegación.
/// </summary>
/// <typeparam name="T">Tipo de los elementos retornados.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Registros de la página actual.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>Número de página actual (base 1).</summary>
    public required int Page { get; init; }

    /// <summary>Cantidad máxima de registros por página.</summary>
    public required int PageSize { get; init; }

    /// <summary>Total de registros que coinciden con el filtro aplicado.</summary>
    public required long TotalCount { get; init; }

    /// <summary>Total de páginas calculado a partir de <see cref="TotalCount"/> y <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Indica si existe una página anterior.</summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>Indica si existe una página siguiente.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Crea una instancia vacía de <see cref="PagedResult{T}"/> para la página solicitada.
    /// Útil cuando no hay resultados que retornar.
    /// </summary>
    public static PagedResult<T> Empty(int page, int pageSize) => new()
    {
        Items = [],
        Page = page,
        PageSize = pageSize,
        TotalCount = 0
    };
}

using System.Linq.Expressions;
using WsUtaSystem.Application.DTOs.Common;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Utilidades para los listados paginados de catálogos: búsqueda por palabras sin importar mayúsculas ni tildes,
/// combinación de filtros y forma estándar de la respuesta paginada.
/// </summary>
public static class CatalogPredicates
{
    /// <summary>Intercalación sin distinción de mayúsculas ni tildes (CI_AI) para comparar texto en SQL Server.</summary>
    public const string Collation = "Latin1_General_100_CI_AI";

    /// <summary>Máximo de registros por página aceptado por los listados.</summary>
    public const int MaxPageSize = 200;

    /// <summary>
    /// Divide el texto buscado en palabras y exige que cada una cumpla <paramref name="perWord"/> (condición AND).
    /// Devuelve null si no hay texto, para no aplicar filtro.
    /// </summary>
    public static Expression<Func<T, bool>>? ByWords<T>(string? search, Func<string, Expression<Func<T, bool>>> perWord)
    {
        var words = (search ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Expression<Func<T, bool>>? result = null;
        foreach (var word in words)
            result = result.And(perWord(word));
        return result;
    }

    /// <summary>Combina dos filtros con AND; cualquiera de los dos puede ser null (se ignora).</summary>
    public static Expression<Func<T, bool>>? And<T>(this Expression<Func<T, bool>>? left, Expression<Func<T, bool>>? right)
    {
        if (left is null) return right;
        if (right is null) return left;

        var parameter = Expression.Parameter(typeof(T), "x");
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    /// <summary>Normaliza página y tamaño de página a rangos válidos (1.. y 1-200).</summary>
    public static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (page < 1 ? 1 : page, pageSize < 1 || pageSize > MaxPageSize ? 20 : pageSize);

    /// <summary>Forma estándar de la respuesta paginada (la misma que ya usa /jobs/paged).</summary>
    public static object ToResponse<TEntity, TDto>(PagedResult<TEntity> paged, List<TDto> items) => new
    {
        items,
        page = paged.Page,
        pageSize = paged.PageSize,
        totalCount = paged.TotalCount,
        totalPages = paged.TotalPages,
        hasPreviousPage = paged.HasPreviousPage,
        hasNextPage = paged.HasNextPage
    };

    private sealed class ReplaceParameterVisitor(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : base.VisitParameter(node);
    }
}

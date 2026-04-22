using WsUtaSystem.Application.DTOs.Documents.Templates;
using WsUtaSystem.Models;

namespace WsUtaSystem.Documents.Abstractions;

/// <summary>
/// Resuelve los valores de los campos de una plantilla documental
/// consultando las fuentes de datos correspondientes (EMPLOYEE, CONTRACT, MOVEMENT, SYSTEM).
/// </summary>
public interface IDocumentFieldResolver
{
    /// <summary>
    /// Resuelve todos los campos de una plantilla para un empleado y entidad específica.
    /// Devuelve un diccionario de <c>fieldName → valor resuelto</c>.
    /// </summary>
    /// <param name="fields">Campos definidos en la plantilla.</param>
    /// <param name="employeeId">ID del empleado afectado.</param>
    /// <param name="entityId">ID de la entidad relacionada (contrato, movimiento, acción).</param>
    /// <param name="overrides">Valores manuales que sobreescriben la resolución automática.</param>
    /// <param name="ct">Token de cancelación.</param>
    Task<Dictionary<string, string>> ResolveAsync(
        IReadOnlyList<DocumentTemplateField> fields,
        int employeeId,
        int? entityId,
        Dictionary<string, string>? overrides = null,
        CancellationToken ct = default);
}

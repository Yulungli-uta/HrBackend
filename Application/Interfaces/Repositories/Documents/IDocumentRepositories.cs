using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.DTOs.Documents.PersonnelActions;
using WsUtaSystem.Application.DTOs.Documents.Templates;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories.Documents;

// ── IDocumentTemplateRepository ──────────────────────────────────────────────────

/// <summary>
/// Repositorio de plantillas documentales.
/// Provee acceso de lectura y escritura a <c>tbl_DocumentTemplates</c>
/// y <c>tbl_DocumentTemplateFields</c>.
/// </summary>
public interface IDocumentTemplateRepository
{
    /// <summary>Obtiene todas las plantillas con resumen de campos.</summary>
    Task<IReadOnlyList<DocumentTemplateSummaryDto>> GetAllAsync(
        string? templateType = null,
        DocumentTemplateStatus? status = null,
        CancellationToken ct = default);

    /// <summary>Obtiene el detalle completo de una plantilla incluyendo sus campos.</summary>
    Task<DocumentTemplateDetailDto?> GetDetailByIdAsync(int templateId, CancellationToken ct = default);

    /// <summary>Obtiene una plantilla por su código único.</summary>
    Task<DocumentTemplate?> GetByCodeAsync(string templateCode, CancellationToken ct = default);

    /// <summary>Obtiene la entidad completa para operaciones de escritura.</summary>
    Task<DocumentTemplate?> GetByIdAsync(int templateId, CancellationToken ct = default);

    /// <summary>Verifica si existe una plantilla con el código dado.</summary>
    Task<bool> ExistsByCodeAsync(string templateCode, int? excludeId = null, CancellationToken ct = default);

    /// <summary>Crea una nueva plantilla con sus campos.</summary>
    Task<int> CreateAsync(DocumentTemplate template, CancellationToken ct = default);

    /// <summary>Actualiza los datos de una plantilla existente.</summary>
    Task UpdateAsync(DocumentTemplate template, CancellationToken ct = default);

    /// <summary>Cambia el estado de una plantilla.</summary>
    Task UpdateStatusAsync(int templateId, DocumentTemplateStatus status, CancellationToken ct = default);
}

// ── IDocumentTemplateFieldRepository ────────────────────────────────────────────

/// <summary>
/// Repositorio de campos de plantillas documentales.
/// </summary>
public interface IDocumentTemplateFieldRepository
{
    /// <summary>Obtiene todos los campos de una plantilla ordenados por <c>SortOrder</c>.</summary>
    Task<IReadOnlyList<DocumentTemplateField>> GetByTemplateIdAsync(int templateId, CancellationToken ct = default);

    /// <summary>Obtiene un campo por su ID.</summary>
    Task<DocumentTemplateField?> GetByIdAsync(int fieldId, CancellationToken ct = default);

    /// <summary>Verifica si existe un campo con el mismo nombre en la plantilla.</summary>
    Task<bool> ExistsByNameAsync(int templateId, string fieldName, int? excludeId = null, CancellationToken ct = default);

    /// <summary>Crea un nuevo campo.</summary>
    Task<int> CreateAsync(DocumentTemplateField field, CancellationToken ct = default);

    /// <summary>Actualiza un campo existente.</summary>
    Task UpdateAsync(DocumentTemplateField field, CancellationToken ct = default);

    /// <summary>Elimina un campo por su ID.</summary>
    Task DeleteAsync(int fieldId, CancellationToken ct = default);

    /// <summary>Reemplaza todos los campos de una plantilla (usado en actualización masiva).</summary>
    Task ReplaceFieldsAsync(int templateId, IEnumerable<DocumentTemplateField> fields, CancellationToken ct = default);
}

// ── IGeneratedDocumentRepository ────────────────────────────────────────────────

/// <summary>
/// Repositorio de documentos generados.
/// </summary>
public interface IGeneratedDocumentRepository
{
    /// <summary>Obtiene documentos paginados con filtros.</summary>
    Task<PagedDocumentResult> GetPagedAsync(DocumentQueryFilter filter, CancellationToken ct = default);

    /// <summary>Obtiene el detalle completo de un documento generado.</summary>
    Task<GeneratedDocumentDetailDto?> GetDetailByIdAsync(int documentId, CancellationToken ct = default);

    /// <summary>Obtiene la entidad completa para operaciones de escritura.</summary>
    Task<GeneratedDocument?> GetByIdAsync(int documentId, CancellationToken ct = default);

    /// <summary>Obtiene todos los documentos de un empleado.</summary>
    Task<IReadOnlyList<GeneratedDocumentSummaryDto>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);

    /// <summary>Crea un documento generado con su snapshot de campos.</summary>
    Task<int> CreateAsync(GeneratedDocument document, CancellationToken ct = default);

    /// <summary>Actualiza el estado y notas de un documento.</summary>
    Task UpdateStatusAsync(int documentId, string status, string? notes, CancellationToken ct = default);

    /// <summary>Registra la firma del documento.</summary>
    Task SignAsync(int documentId, int signedBy, CancellationToken ct = default);

    /// <summary>Registra la aprobación del documento.</summary>
    Task ApproveAsync(int documentId, int approvedBy, string? notes, CancellationToken ct = default);

    /// <summary>Actualiza el StoredFileId después de guardar el PDF.</summary>
    Task UpdateStoredFileAsync(int documentId, int storedFileId, string fileName, CancellationToken ct = default);
}

// ── IPersonnelActionRepository ───────────────────────────────────────────────────

/// <summary>
/// Repositorio de acciones de personal.
/// </summary>
public interface IPersonnelActionRepository
{
    /// <summary>Obtiene acciones de personal paginadas con filtros.</summary>
    Task<PagedPersonnelActionResult> GetPagedAsync(PersonnelActionQueryFilter filter, CancellationToken ct = default);

    /// <summary>Obtiene el detalle completo de una acción de personal.</summary>
    Task<PersonnelActionDetailDto?> GetDetailByIdAsync(int actionId, CancellationToken ct = default);

    /// <summary>Obtiene la entidad completa para operaciones de escritura.</summary>
    Task<PersonnelAction?> GetByIdAsync(int actionId, CancellationToken ct = default);

    /// <summary>Obtiene todas las acciones de un empleado.</summary>
    Task<IReadOnlyList<PersonnelActionSummaryDto>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);

    /// <summary>Crea una nueva acción de personal.</summary>
    Task<int> CreateAsync(PersonnelAction action, CancellationToken ct = default);

    /// <summary>Actualiza los datos de una acción de personal.</summary>
    Task UpdateAsync(PersonnelAction action, CancellationToken ct = default);

    /// <summary>Actualiza el estado de una acción.</summary>
    Task UpdateStatusAsync(int actionId, string status, CancellationToken ct = default);

    /// <summary>Vincula un documento generado a una acción de personal.</summary>
    Task LinkDocumentAsync(int actionId, int documentId, CancellationToken ct = default);
}

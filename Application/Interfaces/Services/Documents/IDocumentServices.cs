using WsUtaSystem.Application.DTOs.Documents.GeneratedDocuments;
using WsUtaSystem.Application.DTOs.Documents.Templates;
using WsUtaSystem.Application.Common.Enums;
using WsUtaSystem.Application.DTOs.PersonnelActions;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;

namespace WsUtaSystem.Application.Interfaces.Services.Documents;

// ── IDocumentTemplateService ─────────────────────────────────────────────────────

/// <summary>
/// Servicio de gestión del ciclo de vida de plantillas documentales.
/// </summary>
public interface IDocumentTemplateService
{
    /// <summary>Obtiene el listado de plantillas con filtros opcionales.</summary>
    Task<IReadOnlyList<DocumentTemplateSummaryDto>> GetAllAsync(
        string? templateType = null,
        DocumentTemplateStatus? status = null,
        CancellationToken ct = default);

    /// <summary>Obtiene el detalle completo de una plantilla.</summary>
    Task<DocumentTemplateDetailDto> GetDetailByIdAsync(int templateId, CancellationToken ct = default);

    /// <summary>Crea una nueva plantilla con sus campos.</summary>
    Task<int> CreateAsync(CreateDocumentTemplateRequest request, int createdBy, CancellationToken ct = default);

    /// <summary>Actualiza los datos de una plantilla existente.</summary>
    Task UpdateAsync(int templateId, UpdateDocumentTemplateRequest request, int updatedBy, CancellationToken ct = default);

    /// <summary>Cambia el estado de una plantilla (Draft → Published → Archived).</summary>
    Task ChangeStatusAsync(int templateId, ChangeTemplateStatusRequest request, int updatedBy, CancellationToken ct = default);

    /// <summary>Previsualiza el HTML de una plantilla con datos reales o de muestra.</summary>
    Task<PreviewTemplateResponse> PreviewAsync(PreviewTemplateRequest request, CancellationToken ct = default);
}

// ── IDocumentTemplateFieldService ────────────────────────────────────────────────

/// <summary>
/// Servicio de gestión de campos de plantillas documentales.
/// </summary>
public interface IDocumentTemplateFieldService
{
    /// <summary>Obtiene todos los campos de una plantilla.</summary>
    Task<IReadOnlyList<DocumentTemplateFieldDto>> GetByTemplateIdAsync(int templateId, CancellationToken ct = default);

    /// <summary>Crea un nuevo campo en una plantilla.</summary>
    Task<int> CreateAsync(int templateId, CreateDocumentTemplateFieldRequest request, int createdBy, CancellationToken ct = default);

    /// <summary>Actualiza un campo existente.</summary>
    Task UpdateAsync(int fieldId, UpdateDocumentTemplateFieldRequest request, int updatedBy, CancellationToken ct = default);

    /// <summary>Elimina un campo de una plantilla.</summary>
    Task DeleteAsync(int fieldId, CancellationToken ct = default);
}

// ── IDocumentGenerationService ───────────────────────────────────────────────────

/// <summary>
/// Servicio de generación de documentos PDF a partir de plantillas.
/// Orquesta la resolución de campos, sustitución de placeholders y renderizado PDF.
/// </summary>
public interface IDocumentGenerationService
{
    /// <summary>
    /// Genera un documento PDF a partir de una plantilla y datos de contexto.
    /// Guarda el snapshot de campos y el archivo PDF.
    /// </summary>
    Task<GenerateDocumentResponse> GenerateAsync(GenerateDocumentRequest request, int generatedBy, CancellationToken ct = default);

    /// <summary>Obtiene documentos generados paginados con filtros.</summary>
    Task<PagedDocumentResult> GetPagedAsync(DocumentQueryFilter filter, CancellationToken ct = default);

    /// <summary>Obtiene el detalle completo de un documento generado.</summary>
    Task<GeneratedDocumentDetailDto> GetDetailByIdAsync(int documentId, CancellationToken ct = default);

    /// <summary>Descarga el PDF de un documento generado.</summary>
    Task<(byte[] Bytes, string FileName, string ContentType)> DownloadAsync(int documentId, CancellationToken ct = default);

    /// <summary>Aprueba un documento generado.</summary>
    Task ApproveAsync(int documentId, ApproveDocumentRequest request, int approvedBy, CancellationToken ct = default);

    /// <summary>Actualiza el estado de un documento.</summary>
    Task UpdateStatusAsync(int documentId, UpdateDocumentStatusRequest request, int updatedBy, CancellationToken ct = default);

    /// <summary>
    /// Genera un PDF de previsualización sin guardar ningún registro en la BD.
    /// Útil para revisar el aspecto del documento antes de crear la acción.
    /// </summary>
    Task<string> PreviewAsync(int templateId, int employeeId, Dictionary<string, string> overrides, CancellationToken ct = default);
}

// ── IPersonnelActionService ──────────────────────────────────────────────────────

/// <summary>
/// Servicio de gestión de acciones de personal (LOSEP / RLOSEP).
/// Integra la creación de la acción con la generación automática del documento PDF.
/// </summary>
public interface IPersonnelActionService
{
    /// <summary>Obtiene acciones de personal paginadas con filtros.</summary>
    Task<PagedPersonnelActionResult> GetPagedAsync(PersonnelActionQueryFilter filter, CancellationToken ct = default);

    /// <summary>Obtiene el detalle completo de una acción de personal.</summary>
    Task<PersonnelActionDetailDto> GetDetailByIdAsync(int actionId, CancellationToken ct = default);

    /// <summary>Obtiene todas las acciones de un empleado.</summary>
    Task<IReadOnlyList<PersonnelActionSummaryDto>> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Crea una nueva acción de personal.
    /// Si <c>GenerateDocument = true</c>, genera automáticamente el PDF.
    /// </summary>
    Task<CreatePersonnelActionResponse> CreateAsync(CreatePersonnelActionRequest request, int createdBy, CancellationToken ct = default);

    /// <summary>Actualiza los datos de una acción de personal en estado DRAFT.</summary>
    Task UpdateAsync(int actionId, UpdatePersonnelActionRequest request, int updatedBy, CancellationToken ct = default);

    /// <summary>Aprueba y ejecuta una acción de personal.</summary>
    Task<CreatePersonnelActionResponse> ApproveAsync(int actionId, ApprovePersonnelActionRequest request, int approvedBy, CancellationToken ct = default);

    /// <summary>Genera o regenera el documento PDF de una acción de personal.</summary>
    Task<CreatePersonnelActionResponse> GenerateDocumentAsync(int actionId, Dictionary<string, string>? overrides, int generatedBy, CancellationToken ct = default);

    /// <summary>
    /// Transición GENERADO → PENDIENTE_FIRMAS.
    /// Indica que el documento fue impreso y está pendiente de firma física.
    /// </summary>
    Task MarkPendingSignaturesAsync(int actionId, string? comment, int updatedBy, CancellationToken ct = default);

    /// <summary>
    /// Transición PENDIENTE_FIRMAS → FIRMADO_CARGADO.
    /// Adjunta el documento firmado escaneado y actualiza el estado.
    /// </summary>
    Task UploadSignedDocumentAsync(int actionId, UploadSignedDocumentRequest request, int updatedBy, CancellationToken ct = default);

    /// <summary>
    /// Transición FIRMADO_CARGADO → FINALIZADO.
    /// Cierra el ciclo de vida de la acción de personal.
    /// </summary>
    Task FinalizeAsync(int actionId, string? comment, int updatedBy, CancellationToken ct = default);

    /// <summary>
    /// Transición cualquier estado activo → ANULADO.
    /// No se puede anular una acción ya FINALIZADA.
    /// </summary>
    Task CancelAsync(int actionId, CancelPersonnelActionRequest request, int updatedBy, CancellationToken ct = default);

    /// <summary>Obtiene el historial de cambios de estado de una acción.</summary>
    Task<IReadOnlyList<PersonnelActionStatusHistoryDto>> GetStatusHistoryAsync(int actionId, CancellationToken ct = default);

    /// <summary>
    /// Genera un PDF de previsualización sin guardar la acción en la BD.
    /// Usa la plantilla ACCION_PERSONAL publicada con los overrides del formulario.
    /// </summary>
    Task<(string PdfBase64, string FileName)> PreviewDocumentAsync(
        int employeeId, Dictionary<string, string> overrides, CancellationToken ct = default);

    /// <summary>
    /// Retorna acciones de personal para reporte con filtros de fecha, estado y categoría.
    /// Permite excluir categorías (ej. DISCIPLINARY) para el historial de empleado.
    /// </summary>
    Task<IReadOnlyList<PersonnelActionReportDto>> GetForReportAsync(ReportFilterDto filter, CancellationToken ct = default);
}

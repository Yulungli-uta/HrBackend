using WsUtaSystem.Application.DTOs.TramiteRequirements;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Parametrización de requisitos documentales por trámite (checklist de obligatoriedad).
/// Catálogo global, no filtrado por persona: el acceso a la lectura y mutación se controla
/// por permiso de acción (TRAMITE_REQUIREMENTS.*), no por asignación individual de módulo.
/// </summary>
public interface ITramiteRequirementsService
{
    /// <summary>Todos los módulos (ACCESS_MODULE_TYPE) parametrizables.</summary>
    Task<List<AccessibleModuleDto>> GetAccessibleModulesAsync(CancellationToken ct);

    /// <summary>Requisitos configurados para un módulo.</summary>
    Task<List<TramiteRequirementDto>> GetByModuleAsync(int moduleTypeId, CancellationToken ct);

    /// <summary>
    /// Requisitos activos aplicables a un módulo y, opcionalmente, a un tipo específico dentro
    /// de él (generales + los propios del tipo específico). Lectura abierta a cualquier usuario
    /// autenticado (no requiere TRAMITE_REQUIREMENTS.READ) porque la consume cualquiera que esté
    /// completando el trámite, no solo quien administra el catálogo.
    /// </summary>
    Task<List<TramiteRequirementDto>> GetApplicableAsync(int moduleTypeId, int? specificTypeId, CancellationToken ct);

    Task<TramiteRequirementDto> CreateAsync(TramiteRequirementCreateDto dto, int? createdBy, CancellationToken ct);

    Task UpdateAsync(int requirementId, TramiteRequirementUpdateDto dto, int? updatedBy, CancellationToken ct);

    Task DeleteAsync(int requirementId, CancellationToken ct);

    /// <summary>
    /// Verifica que, para el módulo y tipo específico dados, existan documentos activos
    /// (HR.tbl_StoredFile) para todo requisito marcado obligatorio. Lanza
    /// <see cref="InvalidOperationException"/> con el detalle de lo faltante si no se cumple.
    /// No falla si no hay ningún requisito configurado (comportamiento actual sin cambios).
    /// </summary>
    Task ValidateRequiredDocumentsAsync(
        int moduleTypeId, int? specificTypeId, string entityType, string entityId, CancellationToken ct);
}

using WsUtaSystem.Application.DTOs.ScientificProviders;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Common;

/// <summary>
/// Reglas de estado de una publicación (revisión y congelamiento). Un solo lugar para decidir qué cuenta en el CV y la
/// promoción docente y qué se puede modificar, para que todos los consumidores apliquen el mismo criterio.
/// </summary>
public static class PublicationRules
{
    /// <summary>Aprobada: congelada. Rechazada: no se modifica (conserva el ID externo para no reimportarla).</summary>
    public static bool IsLocked(string? status) =>
        status is PublicationApprovalStatus.Aprobada or PublicationApprovalStatus.Rechazada;

    /// <summary>
    /// Cuenta en el CV en PDF y en la promoción docente: toda publicación aprobada, y las sin revisión cuyo origen no exige
    /// aprobación (histórico y manual). Lo sincronizado de un proveedor solo cuenta cuando la Dirección lo aprueba.
    /// </summary>
    public static bool CountsForCv(string? status, bool providerRequiresApproval) => status switch
    {
        PublicationApprovalStatus.Aprobada => true,
        PublicationApprovalStatus.SinRevision => !providerRequiresApproval,
        _ => false,
    };

    /// <summary>Lanza si la publicación está congelada (aprobada) o rechazada. <paramref name="action"/>: "modificar" o "eliminar".</summary>
    public static void EnsureEditable(Publications publication, string action)
    {
        if (publication.ApprovalStatus == PublicationApprovalStatus.Aprobada)
            throw new BusinessRuleException(
                $"La publicación fue aprobada por la Dirección de Investigación y está congelada: no se puede {action}. " +
                "Solicite que la reabran si necesita un cambio.");
        if (publication.ApprovalStatus == PublicationApprovalStatus.Rechazada)
            throw new BusinessRuleException(
                $"La publicación fue rechazada por la Dirección de Investigación: no se puede {action}. " +
                "La Dirección puede restaurarla.");
    }

    /// <summary>
    /// Conserva en <paramref name="incoming"/> las columnas que el formulario de edición no envía (origen, ID externo,
    /// estado de revisión y fechas de sincronización). Sin esto la actualización genérica las reiniciaría.
    /// </summary>
    public static void PreserveProtectedColumns(Publications incoming, Publications existing)
    {
        incoming.SourceProviderTypeId = existing.SourceProviderTypeId;
        incoming.ExternalId = existing.ExternalId;
        incoming.DoiNormalized = existing.DoiNormalized;
        incoming.ApprovalStatus = existing.ApprovalStatus;
        incoming.ApprovedAt = existing.ApprovedAt;
        incoming.ApprovedBy = existing.ApprovedBy;
        incoming.ReviewNotes = existing.ReviewNotes;
        incoming.LastSyncedAt = existing.LastSyncedAt;
    }

    /// <summary>Si el proveedor de la publicación exige aprobación (por defecto sí, la opción más restrictiva).</summary>
    public static bool RequiresApproval(Publications publication, IReadOnlyList<ScientificProviderInfo> providers) =>
        providers.FirstOrDefault(p => p.TypeId == publication.SourceProviderTypeId)?.RequiresApproval ?? true;
}

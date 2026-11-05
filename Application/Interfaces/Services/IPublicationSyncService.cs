using WsUtaSystem.Application.DTOs.PublicationSync;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Sincronización de publicaciones de una persona con un proveedor externo (Scopus…): vista previa y confirmación en dos
/// pasos, igual que Capacitaciones (DEaDV) y Formación (SENESCYT). Nunca guarda nada al consultar. Lo importado queda
/// SIN_REVISION hasta que la Dirección de Investigación lo apruebe. La autorización (dueño o rol elevado) la aplica el controlador.
/// </summary>
public interface IPublicationSyncService
{
    /// <summary>Qué le falta a la persona (ORCID, ID de plataforma, configuración) para sincronizar cada proveedor.</summary>
    Task<PublicationSyncReadinessDto> GetReadinessAsync(int personId, bool canEdit, CancellationToken ct);

    /// <summary>Consulta al proveedor y compara con lo registrado, sin guardar nada.</summary>
    Task<PublicationSyncPreviewDto> PreviewAsync(int personId, string providerCode, CancellationToken ct);

    /// <summary>
    /// Aplica la vista previa en una sola transacción, con un candado que impide dos sincronizaciones a la vez de la misma
    /// persona y proveedor. Los casos ambiguos exigen una resolución. Repetirla no crea duplicados.
    /// </summary>
    Task<PublicationSyncResultDto> ConfirmAsync(int personId, PublicationSyncConfirmRequest request, CancellationToken ct);

    /// <summary>
    /// Sincronización sin intervención (corrida masiva): importa lo nuevo y completa vacíos; los ambiguos se omiten y se
    /// informan. Devuelve cómo terminó para decidir si la corrida continúa.
    /// </summary>
    Task<AutomaticSyncOutcome> SyncAutomaticallyAsync(int personId, string providerCode, CancellationToken ct);
}

public sealed record AutomaticSyncOutcome(
    bool Available,
    bool IsPartial,
    int Created,
    int Completed,
    int AmbiguousSkipped,
    int? QuotaRemaining,
    string? ErrorKind,
    string? Message);

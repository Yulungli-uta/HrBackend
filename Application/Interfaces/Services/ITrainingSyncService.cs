using WsUtaSystem.Application.DTOs.Trainings;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Sincroniza Capacitaciones (HR.tbl_Trainings) contra los certificados de DEaDV. Mismo flujo que
/// la sincronización SENESCYT: se previsualiza (cuántos hay, cuántos ya existen, cuántos faltan) y
/// solo al confirmar se guarda. Nunca duplica: el cert_code de DEaDV se guarda como ExternalId y,
/// si ya existe un registro histórico equivalente sin origen, se completa en vez de crear otro.
/// </summary>
public interface ITrainingSyncService
{
    /// <summary>Consulta DEaDV y compara contra lo registrado, SIN persistir nada.</summary>
    Task<DeadvSyncPreviewDto> PreviewAsync(int personId, CancellationToken ct = default);

    /// <summary>Crea los certificados faltantes y completa los registros históricos vinculables.</summary>
    Task<DeadvSyncResultDto> ConfirmAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// PDF del certificado de una capacitación sincronizada, descargado por el backend (DEaDV no
    /// permite incrustarlo desde el navegador). Retorna null si no tiene certificado o no se pudo obtener.
    /// La URL almacenada nunca sale del backend.
    /// </summary>
    Task<byte[]?> GetCertificatePdfAsync(Trainings training, CancellationToken ct = default);
}

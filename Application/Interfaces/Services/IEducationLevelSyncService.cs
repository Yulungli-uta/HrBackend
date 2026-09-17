using WsUtaSystem.Application.DTOs.Dinardap;

namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>
/// Sincroniza Formación Académica (HR.tbl_EducationLevels) contra DINARDAP (Títulos SENESCYT)
/// vía WsUtaDinardap.Api. Dedup por SenescytRegistrationNumber — nunca duplica un título ya
/// registrado, solo agrega los que faltan. Flujo aprobado 2026-09-16: previsualizar antes de
/// confirmar, tanto para una persona puntual como para el lote masivo de activos.
/// </summary>
public interface IEducationLevelSyncService
{
    /// <summary>Consulta DINARDAP y compara contra lo ya registrado, SIN persistir nada todavía.</summary>
    Task<TituloSyncPreviewDto> PreviewAsync(int personId, CancellationToken ct = default);

    /// <summary>Repite la consulta y crea únicamente los títulos que no existían.</summary>
    Task<TituloSyncResultDto> ConfirmAsync(int personId, CancellationToken ct = default);

    /// <summary>
    /// Modo masivo: recorre todos los empleados activos, sincronizando cada uno (sin
    /// previsualización individual). Un fallo puntual de DINARDAP para una persona se omite y
    /// continúa con las demás - nunca aborta el lote completo.
    /// </summary>
    Task<BulkTituloSyncResultDto> SyncActiveEmployeesAsync(CancellationToken ct = default);
}

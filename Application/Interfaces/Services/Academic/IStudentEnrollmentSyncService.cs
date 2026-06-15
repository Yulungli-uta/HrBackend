namespace WsUtaSystem.Application.Interfaces.Services.Academic;

/// <summary>
/// Orquesta la sincronización entre el origen de datos de matrícula y el aprovisionamiento AD.
/// </summary>
public interface IStudentEnrollmentSyncService
{
    /// <summary>
    /// Para cada estudiante matriculado en <paramref name="periodCode"/>:
    /// busca o crea su Person y Student en HR, luego aprovisiona la cuenta AD si no existe.
    /// Retorna el número de cuentas creadas/actualizadas.
    /// </summary>
    Task<int> SyncPeriodAsync(string periodCode, string serviceToken, CancellationToken ct = default);

    /// <summary>
    /// Deshabilita las cuentas AD de estudiantes que no se re-matricularon en el período actual.
    /// Retorna el número de cuentas deshabilitadas.
    /// </summary>
    Task<int> DisableNonReEnrolledAsync(
        string currentPeriod,
        string previousPeriod,
        string serviceToken,
        CancellationToken ct = default);
}

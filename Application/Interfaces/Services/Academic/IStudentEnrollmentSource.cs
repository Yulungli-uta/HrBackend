using WsUtaSystem.Application.Infrastructure.Academic;

namespace WsUtaSystem.Application.Interfaces.Services.Academic;

/// <summary>
/// Abstrae el origen de los datos de matrícula de estudiantes.
/// Permite intercambiar entre una tabla/vista SQL y una API REST externa
/// cambiando una sola línea en el contenedor DI.
/// </summary>
public interface IStudentEnrollmentSource
{
    /// <summary>
    /// Retorna los estudiantes matriculados para el período indicado.
    /// </summary>
    Task<IReadOnlyList<StudentEnrollmentRecord>> GetEnrolledAsync(string periodCode, CancellationToken ct = default);

    /// <summary>
    /// Retorna los estudiantes que estuvieron activos en <paramref name="previousPeriod"/>
    /// pero NO están en <paramref name="currentPeriod"/> (no se re-matricularon).
    /// </summary>
    Task<IReadOnlyList<StudentEnrollmentRecord>> GetNonReEnrolledAsync(
        string currentPeriod,
        string previousPeriod,
        CancellationToken ct = default);
}

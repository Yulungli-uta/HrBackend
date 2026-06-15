using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services.Academic;

namespace WsUtaSystem.Application.Infrastructure.Academic;

/// <summary>
/// Origen de datos de matrícula basado en tabla/vista SQL del sistema académico.
/// Stub: implementar cuando la estructura de la tabla/vista esté definida.
/// Para cambiar a la API externa, reemplazar el registro DI por ApiStudentEnrollmentSource.
/// </summary>
public class DbStudentEnrollmentSource : IStudentEnrollmentSource
{
    private readonly ILogger<DbStudentEnrollmentSource> _logger;

    public DbStudentEnrollmentSource(ILogger<DbStudentEnrollmentSource> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<StudentEnrollmentRecord>> GetEnrolledAsync(string periodCode, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[DbStudentEnrollmentSource] GetEnrolledAsync no implementado. PeriodCode={PeriodCode}", periodCode);

        return Task.FromResult<IReadOnlyList<StudentEnrollmentRecord>>([]);
    }

    public Task<IReadOnlyList<StudentEnrollmentRecord>> GetNonReEnrolledAsync(
        string currentPeriod,
        string previousPeriod,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[DbStudentEnrollmentSource] GetNonReEnrolledAsync no implementado. Current={Current} Previous={Previous}",
            currentPeriod, previousPeriod);

        return Task.FromResult<IReadOnlyList<StudentEnrollmentRecord>>([]);
    }
}

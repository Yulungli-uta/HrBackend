using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services.Academic;

namespace WsUtaSystem.Application.Infrastructure.Academic;

/// <summary>
/// Origen de datos de matrícula basado en API REST del sistema académico externo.
/// Stub: implementar cuando el contrato de la API esté definido.
/// Para activar, cambiar el registro DI de IStudentEnrollmentSource a esta clase.
/// </summary>
public class ApiStudentEnrollmentSource : IStudentEnrollmentSource
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiStudentEnrollmentSource> _logger;

    public ApiStudentEnrollmentSource(
        HttpClient http,
        ILogger<ApiStudentEnrollmentSource> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public Task<IReadOnlyList<StudentEnrollmentRecord>> GetEnrolledAsync(string periodCode, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[ApiStudentEnrollmentSource] GetEnrolledAsync no implementado. PeriodCode={PeriodCode}", periodCode);

        return Task.FromResult<IReadOnlyList<StudentEnrollmentRecord>>([]);
    }

    public Task<IReadOnlyList<StudentEnrollmentRecord>> GetNonReEnrolledAsync(
        string currentPeriod,
        string previousPeriod,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[ApiStudentEnrollmentSource] GetNonReEnrolledAsync no implementado. Current={Current} Previous={Previous}",
            currentPeriod, previousPeriod);

        return Task.FromResult<IReadOnlyList<StudentEnrollmentRecord>>([]);
    }
}

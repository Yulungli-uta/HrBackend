using WsUtaSystem.Application.DTOs.EmployeeCertificate;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IEmployeeCertificateService
{
    /// <summary>Crea y emite (auto-genera el PDF) una solicitud de certificado para el empleado autenticado.</summary>
    Task<EmployeeCertificateDetailDto> CreateAsync(int employeeId, CreateEmployeeCertificateRequest request, CancellationToken ct = default);

    Task<PagedEmployeeCertificateResult> GetMyRequestsAsync(int employeeId, EmployeeCertificateQueryFilter filter, CancellationToken ct = default);

    Task<EmployeeCertificateDetailDto> GetMyRequestDetailAsync(int requestId, int employeeId, CancellationToken ct = default);

    Task<PagedEmployeeCertificateResult> GetPagedAsync(EmployeeCertificateQueryFilter filter, CancellationToken ct = default);

    Task<EmployeeCertificateDetailDto> GetDetailByIdAsync(int requestId, CancellationToken ct = default);

    /// <summary>Descarga el PDF del certificado emitido, validando que pertenezca al empleado autenticado.</summary>
    Task<(byte[] Bytes, string FileName, string ContentType)> DownloadMyDocumentAsync(int requestId, int employeeId, CancellationToken ct = default);

    /// <summary>Descarga administrativa (RRHH), sin verificación de pertenencia por EmployeeId (se valida scope de departamento en el controller).</summary>
    Task<(byte[] Bytes, string FileName, string ContentType)> DownloadAsync(int requestId, CancellationToken ct = default);
}

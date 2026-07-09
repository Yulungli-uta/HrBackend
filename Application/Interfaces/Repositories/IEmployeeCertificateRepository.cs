using WsUtaSystem.Application.DTOs.EmployeeCertificate;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IEmployeeCertificateRepository
{
    Task<int?> GetPublishedTemplateIdAsync(string templateCode, CancellationToken ct = default);

    Task<(string? JobDescription, string? DepartmentName, int? DepartmentId)> GetCurrentPositionAsync(int employeeId, CancellationToken ct = default);

    /// <summary>
    /// Historial laboral completo: todos los contratos (por PersonId) y todas las acciones
    /// de personal (por EmployeeId), para el certificado de historial laboral.
    /// </summary>
    Task<IReadOnlyList<EmploymentHistoryEntry>> GetEmploymentHistoryAsync(int employeeId, CancellationToken ct = default);

    Task<EmployeeCertificateRequest?> GetTrackedByIdAsync(int requestId, CancellationToken ct = default);

    Task<EmployeeCertificateDetailDto?> GetDetailByIdAsync(int requestId, CancellationToken ct = default);

    Task<PagedEmployeeCertificateResult> GetPagedAsync(EmployeeCertificateQueryFilter filter, CancellationToken ct = default);

    Task AddAsync(EmployeeCertificateRequest entity, CancellationToken ct = default);

    Task AddHistoryAsync(EmployeeCertificateStatusHistory history, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}

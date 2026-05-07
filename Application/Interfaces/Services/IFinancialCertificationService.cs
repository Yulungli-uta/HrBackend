using WsUtaSystem.Application.DTOs.FinancialCertification;
using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IFinancialCertificationService : IService<FinancialCertification, int>
{
    Task<IEnumerable<FinancialCertificationDto>> GetPendingAsync(CancellationToken ct = default);
    Task<PagedFinancialCertificationResult> GetPagedAsync(FinancialCertificationQueryFilter filter, CancellationToken ct = default);
    Task ApproveAsync(int certificationId, int userId, CancellationToken ct = default);
    Task RejectAsync(int certificationId, string? reason, int userId, CancellationToken ct = default);
    Task<FinancialCertificationDto?> GetDetailAsync(int certificationId, CancellationToken ct = default);
}

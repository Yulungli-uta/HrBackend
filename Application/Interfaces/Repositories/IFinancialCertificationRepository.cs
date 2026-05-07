using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Application.Interfaces.Repositories;

public interface IFinancialCertificationRepository : IRepository<FinancialCertification, int>
{
    Task<IEnumerable<FinancialCertification>> GetByRequestIdAsync(int requestId, CancellationToken ct = default);
    Task<FinancialCertification?> GetApprovedByRequestIdAsync(int requestId, CancellationToken ct = default);
    Task<IEnumerable<FinancialCertification>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default);
}

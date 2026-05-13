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

    /// <summary>Rechaza temporalmente la certificación. El solicitante puede corregir y reenviar.</summary>
    Task RejectTemporaryAsync(int certificationId, string? reason, int userId, CancellationToken ct = default);

    /// <summary>El solicitante reenvía la certificación corregida; vuelve a PENDIENTE_REVISION.</summary>
    Task ResendAsync(int certificationId, int userId, CancellationToken ct = default);

    Task<FinancialCertificationDto?> GetDetailAsync(int certificationId, CancellationToken ct = default);
}

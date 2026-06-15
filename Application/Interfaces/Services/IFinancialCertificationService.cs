using WsUtaSystem.Application.DTOs.FinancialCertification;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Application.DTOs.Reports.Common;
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

    /// <summary>
    /// Retorna certificaciones financieras para reporte con filtros de estado y fecha de certificación.
    /// Incluye solicitud de contrato asociada, dependencia y razón de rechazo si aplica.
    /// </summary>
    Task<IReadOnlyList<CertificationReportDto>> GetForReportAsync(ReportFilterDto filter, CancellationToken ct = default);
}

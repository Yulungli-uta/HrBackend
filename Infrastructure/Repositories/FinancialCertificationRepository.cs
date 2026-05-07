using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class FinancialCertificationRepository : ServiceAwareEfRepository<FinancialCertification, int>, IFinancialCertificationRepository
{
    public FinancialCertificationRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<IEnumerable<FinancialCertification>> GetByRequestIdAsync(int requestId, CancellationToken ct = default)
        => await _db.Set<FinancialCertification>()
            .AsNoTracking()
            .Where(x => x.RequestId == requestId)
            .OrderByDescending(x => x.CertificationId)
            .ToListAsync(ct);

    public async Task<FinancialCertification?> GetApprovedByRequestIdAsync(int requestId, CancellationToken ct = default)
    {
        var approvedName = "APROBADA";
        return await _db.Set<FinancialCertification>()
            .AsNoTracking()
            .Where(x => x.RequestId == requestId)
            .Join(
                _db.Set<RefTypes>().Where(r => r.Category == "FIN_CERT_STATUS" && r.Name == approvedName),
                cert => cert.Status,
                rt => (int?)rt.TypeId,
                (cert, _) => cert)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<FinancialCertification>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        => await _db.Set<FinancialCertification>()
            .AsNoTracking()
            .Where(x => x.Status == statusTypeId)
            .OrderByDescending(x => x.CertificationId)
            .ToListAsync(ct);
}

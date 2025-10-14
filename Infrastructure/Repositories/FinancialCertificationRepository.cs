using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class FinancialCertificationRepository : ServiceAwareEfRepository<FinancialCertification, int>, IFinancialCertificationRepository
{
    public FinancialCertificationRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}


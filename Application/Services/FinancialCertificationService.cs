using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class FinancialCertificationService : Service<FinancialCertification, int>, IFinancialCertificationService
{
    public FinancialCertificationService(IFinancialCertificationRepository repo) : base(repo) { }
}


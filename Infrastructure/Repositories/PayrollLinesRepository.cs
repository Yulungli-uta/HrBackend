using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PayrollLinesRepository : ServiceAwareEfRepository<PayrollLines, int>, IPayrollLinesRepository
{
    public PayrollLinesRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}

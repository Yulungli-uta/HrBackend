using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;
public class PayrollRepository : ServiceAwareEfRepository<Payroll, int>, IPayrollRepository
{
    public PayrollRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}

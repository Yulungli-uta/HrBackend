using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PayrollService : Service<Payroll, int>, IPayrollService
{
    public PayrollService(IPayrollRepository repo) : base(repo) { }
}

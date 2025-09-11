using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PayrollLinesService : Service<PayrollLines, int>, IPayrollLinesService
{
    public PayrollLinesService(IPayrollLinesRepository repo) : base(repo) { }
}

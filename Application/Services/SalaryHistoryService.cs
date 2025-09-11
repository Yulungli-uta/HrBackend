using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class SalaryHistoryService : Service<SalaryHistory, int>, ISalaryHistoryService
{
    public SalaryHistoryService(ISalaryHistoryRepository repo) : base(repo) { }
}

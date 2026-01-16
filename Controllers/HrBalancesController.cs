using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers
{
    [ApiController]
    [Route("balances")]
    public class HrBalancesController : ControllerBase
    {
        private readonly IHrBalanceService _svc;

        public HrBalancesController(IHrBalanceService svc) => _svc = svc;

        [HttpGet("{employeeId:int}")]
        public async Task<IActionResult> Get(int employeeId)
        {
            var (bal, movs) = await _svc.GetBalancesAsync(employeeId);
            return Ok(new { bal, movs });
        }

        [HttpPost("{employeeId:int}/accrual/daily")]
        public async Task<IActionResult> DailyAccrual(int employeeId)
        {
            var res = await _svc.RunDailyAccrualAsync(employeeId, null, null);
            return res.Ok ? Ok(res) : BadRequest(res);
        }
    }
}

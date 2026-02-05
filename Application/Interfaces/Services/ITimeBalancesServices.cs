using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface ITimeBalancesService : IService<TimeBalances, int> {

    Task CalculateAccrueVacationBalance(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);

    Task<(int StatusCode, string Message)> CalculateAccrueVacationBalanceFinal(
        int? employeeId,
        DateTime asOfDate,
        string mode,
        int? performedByEmpId,
        CancellationToken ct = default
    );

    Task CalculateAccrueVacationBalanceAllEmployees(
        DateTime asOfDate,
        string mode,
        int? performedByEmpId,
        CancellationToken ct = default
    );
}



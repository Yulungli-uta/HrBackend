using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class TimePlanningExecutionService : Service<TimePlanningExecution, int>, ITimePlanningExecutionService
    {
        private readonly ITimePlanningExecutionRepository _repository;
        private readonly IHolidayService _holidayService;

        public TimePlanningExecutionService(ITimePlanningExecutionRepository repo,
                                          IHolidayService holidayService) : base(repo)
        {
            _repository = repo;
            _holidayService = holidayService;
        }

        public async Task<IEnumerable<TimePlanningExecution>> GetByPlanEmployeeIdAsync(int planEmployeeId, CancellationToken ct = default)
        {
            return await _repository.GetByPlanEmployeeIdAsync(planEmployeeId, ct);
        }

        public async Task<IEnumerable<TimePlanningExecution>> GetByEmployeeAndDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _repository.GetByDateRangeAsync(employeeId, startDate, endDate, ct);
        }

        public async Task<TimePlanningExecution> RegisterWorkTimeAsync(int planEmployeeId, DateTime workDate, DateTime startTime, DateTime endTime, string? comments = null, CancellationToken ct = default)
        {
            var execution = new TimePlanningExecution
            {
                PlanEmployeeID = planEmployeeId,
                WorkDate = workDate,
                StartTime = startTime,
                EndTime = endTime,
                Comments = comments,
                CreatedAt = DateTime.UtcNow
            };

            execution.TotalMinutes = (int)(endTime - startTime).TotalMinutes;

            if (await _holidayService.IsHolidayAsync(workDate, ct))
                execution.HolidayMinutes = execution.TotalMinutes;

            //return await _repository.AddAsync(execution, ct);
            return null;
        }

        public async Task<TimePlanningExecution> VerifyExecutionAsync(int executionId, int verifiedBy, string? comments = null, CancellationToken ct = default)
        {
            var success = await _repository.VerifyExecutionAsync(executionId, verifiedBy, comments, ct);
            if (!success)
                throw new InvalidOperationException("No se pudo verificar la ejecución");

            return await _repository.GetByIdAsync(executionId, ct);
        }

        public async Task<bool> BulkVerifyExecutionsAsync(int planId, int verifiedBy, CancellationToken ct = default)
        {
            var executions = await _repository.GetByPlanIdAsync(planId, ct);
            var tasks = executions.Select(e =>
                _repository.VerifyExecutionAsync(e.ExecutionID, verifiedBy, null, ct));

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
    }
}

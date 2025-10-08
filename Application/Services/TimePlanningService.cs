using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.TimePlanning;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class TimePlanningService : Service<TimePlanning, int>, ITimePlanningService
    {
        private readonly ITimePlanningRepository _repository;
        private readonly ITimePlanningEmployeeRepository _timePlanningEmployeeRepository;

        public TimePlanningService(ITimePlanningRepository repo,
                                 ITimePlanningEmployeeRepository timePlanningEmployeeRepository) : base(repo)
        {
            _repository = repo;
            _timePlanningEmployeeRepository = timePlanningEmployeeRepository;
        }

        public async Task<IEnumerable<TimePlanning>> GetByEmployeeAsync(int employeeId, CancellationToken ct = default)
        {
            return await _repository.GetByEmployeeAsync(employeeId, ct);
        }

        public async Task<IEnumerable<TimePlanning>> GetByStatusAsync(int statusTypeId, CancellationToken ct = default)
        {
            return await _repository.GetByStatusAsync(statusTypeId, ct);
        }

        public async Task<IEnumerable<TimePlanning>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _repository.GetByDateRangeAsync(startDate, endDate, ct);
        }

        //public async Task<PagedResultDTO<TimePlanning>> SearchAsync(TimePlanningSearchDTO searchDto, CancellationToken ct = default)
        //{
        //    var items = await _repository.SearchAsync(searchDto, ct);
        //    var totalCount = await _repository.CountAsync(ct);

        //    return new PagedResultDTO<TimePlanning>
        //    {
        //        Items = items,
        //        TotalCount = totalCount,
        //        PageNumber = searchDto.PageNumber,
        //        PageSize = searchDto.PageSize
        //    };
        //}

        public async Task<TimePlanning> SubmitForApprovalAsync(int planId, int submittedBy, CancellationToken ct = default)
        {
            var success = await _repository.ChangeStatusAsync(planId, 2, submittedBy, ct);
            if (!success)
                throw new InvalidOperationException("No se pudo enviar la planificación para aprobación");

            return await _repository.GetByIdAsync(planId, ct);
        }

        public async Task<TimePlanning> ApprovePlanningAsync(int planId, int approvedBy, int? secondApprover = null, CancellationToken ct = default)
        {
            var success = await _repository.ChangeStatusAsync(planId, 3, approvedBy, ct);
            if (!success)
                throw new InvalidOperationException("No se pudo aprobar la planificación");

            return await _repository.GetByIdAsync(planId, ct);
        }

        public async Task<TimePlanning> RejectPlanningAsync(int planId, int rejectedBy, string reason, CancellationToken ct = default)
        {
            var success = await _repository.ChangeStatusAsync(planId, 4, rejectedBy, ct);
            if (!success)
                throw new InvalidOperationException("No se pudo rechazar la planificación");

            return await _repository.GetByIdAsync(planId, ct);
        }

        public async Task<bool> ValidatePlanningAsync(TimePlanningCreateDTO createDto, CancellationToken ct = default)
        {
            if (createDto.StartDate >= createDto.EndDate)
                return false;

            if (createDto.StartTime >= createDto.EndTime)
                return false;

            return true;
        }
    }
}

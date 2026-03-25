using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Employees;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Services
{
    public class VwEmployeeCompleteService : IvwEmployeeCompleteService
    {
        private readonly IvwEmployeeCompleteRepository _repository;

        public VwEmployeeCompleteService(IvwEmployeeCompleteRepository repository)
        {
            _repository = repository;
        }
        
        public async Task<IEnumerable<VwEmployeeComplete>> GetAllEmployeesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<VwEmployeeComplete> GetEmployeeByIdAsync(int employeeId)
        {
            return await _repository.GetByIdAsync(employeeId);
        }

        public async Task<IEnumerable<VwEmployeeComplete>> GetEmployeesByDepartmentAsync(string department)
        {
            return await _repository.GetByDepartmentAsync(department);
        }

        public async Task<PagedResult<VwEmployeeComplete>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
        {
            return await _repository.GetPagedAsync(page, pageSize, ct);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<VwEmployeeComplete>> GetPagedAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken ct = default)
        {
            return await _repository.GetPagedAsync(search, page, pageSize, ct);
        }

        public async Task<EmployeeCompleteStatsDto> GetStatsAsync(CancellationToken ct = default)
        {
            return await _repository.GetStatsAsync(ct);
        }

        public async Task<List<ContractTypeCountDto>> GetByContractTypeAsync(CancellationToken ct = default)
        {
            return await _repository.GetByContractTypeAsync(ct);
        }


    }
}

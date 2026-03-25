using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Employees;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IvwEmployeeCompleteRepository
    {
        
        Task<IEnumerable<VwEmployeeComplete>> GetAllAsync(CancellationToken ct = default);
        Task<VwEmployeeComplete?> GetByIdAsync(int employeeId, CancellationToken ct = default);
        Task<IEnumerable<VwEmployeeComplete>> GetByDepartmentAsync(string department, CancellationToken ct = default);

        Task<PagedResult<VwEmployeeComplete>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken ct = default);

        Task<PagedResult<VwEmployeeComplete>> GetPagedAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken ct = default);
        Task<EmployeeCompleteStatsDto> GetStatsAsync(CancellationToken ct = default);
        Task<List<ContractTypeCountDto>> GetByContractTypeAsync(CancellationToken ct = default);
    }
}

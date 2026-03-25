using System.Collections.Generic;
using System.Threading.Tasks;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Employees;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IvwEmployeeCompleteService
    {
        Task<IEnumerable<VwEmployeeComplete>> GetAllEmployeesAsync();
        Task<VwEmployeeComplete> GetEmployeeByIdAsync(int employeeId);
        Task<IEnumerable<VwEmployeeComplete>> GetEmployeesByDepartmentAsync(string department);

        /// <summary>Retorna un resultado paginado de empleados.</summary>
        Task<PagedResult<VwEmployeeComplete>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken ct = default);

        /// <summary>Retorna un resultado paginado de empleados con filtro de búsqueda.</summary>
        Task<PagedResult<VwEmployeeComplete>> GetPagedAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken ct = default);

        Task<EmployeeCompleteStatsDto> GetStatsAsync(CancellationToken ct = default);
        Task<List<ContractTypeCountDto>> GetByContractTypeAsync(CancellationToken ct = default);
    }

}

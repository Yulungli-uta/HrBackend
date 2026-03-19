using WsUtaSystem.Models.Views;
using WsUtaSystem.Application.DTOs.Common;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IvwEmployeeDetailsService
  
    {
        Task<VwEmployeeDetails?> GetEmployeeDetailsAsync(
            int employeeId,
            CancellationToken ct = default);
        //Task<PagedResultDto<EmployeeDetailsDto>> GetEmployeeDetailsPagedAsync(
        //    EmployeeDetailsFilterDto filter,
        //    CancellationToken ct = default);
        Task<VwEmployeeDetails?> GetByEmailAsync(string email, CancellationToken ct = default);

        Task<IEnumerable<VwEmployeeDetails>> GetAllEmployeeDetailsAsync(
            CancellationToken ct = default);
        Task<IEnumerable<VwEmployeeDetails>> GetEmployeesByDepartmentAsync(
            string departmentName,
            CancellationToken ct = default);
        Task<IEnumerable<VwEmployeeDetails>> GetEmployeesByFacultyAsync(
            string facultyName,
            CancellationToken ct = default);
        Task<IEnumerable<VwEmployeeDetails>> GetEmployeesByTypeAsync(
            int employeeType,
            CancellationToken ct = default);
        Task<IEnumerable<int>> GetAvailableEmployeeTypesAsync(
            CancellationToken ct = default);
        Task<IEnumerable<string>> GetAvailableDepartmentsAsync(
            CancellationToken ct = default);
        Task<IEnumerable<string>> GetAvailableFacultiesAsync(
            CancellationToken ct = default);
        /// <summary>Retorna un resultado paginado de empleados.</summary>
        Task<PagedResult<VwEmployeeDetails>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken ct = default);

        /// <summary>Retorna un resultado paginado de empleados con filtro de búsqueda.</summary>
        Task<PagedResult<VwEmployeeDetails>> GetPagedAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken ct = default);
    }
}

using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Reports;
using WsUtaSystem.Models.Views;

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

        Task<IEnumerable<VwEmployeeDetails>> GetSubordinatesByBossIdAsync( 
            int bossId, 
            CancellationToken ct = default);

        /*report source*/
        Task<IEnumerable<VwEmployeeDetails>> GetByFiltersAsync(
            int? departmentId,
            int? employeeType,
            CancellationToken ct = default);

        Task<IEnumerable<DepartmentContractCountDto>> GetDepartmentContractCountsAsync(
            int? departmentId,
            int? employeeType,
            CancellationToken ct = default);

        Task<IEnumerable<ScheduleContractCountDto>> GetScheduleContractCountsAsync(
            int? departmentId,
            int? employeeType,
            CancellationToken ct = default);
    }
}

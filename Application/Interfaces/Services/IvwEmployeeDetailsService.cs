using WsUtaSystem.Models;

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
    }
}

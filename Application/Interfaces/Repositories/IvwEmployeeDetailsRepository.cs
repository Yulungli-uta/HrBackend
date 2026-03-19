using WsUtaSystem.Models.Views;
using WsUtaSystem.Application.DTOs.Common;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IvwEmployeeDetailsRepository
    {
         Task<VwEmployeeDetails?> GetByIdAsync(int employeeId, CancellationToken ct = default);

        Task<VwEmployeeDetails?> GetByEmailAsync(string email, CancellationToken ct = default);
        //Task<PagedResultDto<EmployeeDetailsDto>> GetPagedAsync(
        //    EmployeeDetailsFilterDto filter,
        //    CancellationToken ct = default);
        Task<IEnumerable<VwEmployeeDetails>> GetAllAsync(CancellationToken ct = default);
        Task<IEnumerable<VwEmployeeDetails>> GetByDepartmentAsync(
            string departmentName,
            CancellationToken ct = default);
        Task<IEnumerable<VwEmployeeDetails>> GetByFacultyAsync(
            string facultyName,
            CancellationToken ct = default);
        Task<IEnumerable<VwEmployeeDetails>> GetByEmployeeTypeAsync(
            int employeeType,
            CancellationToken ct = default);
        Task<IEnumerable<int>> GetEmployeeTypesAsync(CancellationToken ct = default);
        Task<IEnumerable<string>> GetDepartmentsAsync(CancellationToken ct = default);
        Task<IEnumerable<string>> GetFacultiesAsync(CancellationToken ct = default);
        /// <summary>Retorna un resultado paginado de empleados ordenado por nombre.</summary>
        Task<PagedResult<VwEmployeeDetails>> GetPagedAsync(
            int page,
            int pageSize,
            CancellationToken ct = default);

        /// <summary>Retorna un resultado paginado de empleados con filtro de búsqueda por nombre, apellido o cédula.</summary>
        Task<PagedResult<VwEmployeeDetails>> GetPagedAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken ct = default);
    }
}

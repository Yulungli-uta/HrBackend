using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class VwEmployeeDetailsService : IvwEmployeeDetailsService
    {
        private readonly IvwEmployeeDetailsRepository _repository;
        private readonly ILogger<VwEmployeeDetailsService> _logger;

        public VwEmployeeDetailsService(IvwEmployeeDetailsRepository repository,
             ILogger<VwEmployeeDetailsService> logger)
        {
            _repository = repository;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VwEmployeeDetails?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting employee details for Email: {email}", email);

                if (email.Length <= 0)
                {
                    _logger.LogWarning("Invalid email provided: {email}", email);
                    return null;
                }

                var result = await _repository.GetByEmailAsync(email, ct);

                if (result == null)
                {
                    _logger.LogInformation("Employee not found with ID: {EmployeeId}", email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee details for ID: {EmployeeId}", email);
                throw;
            }
        }

        //public VwEmployeeDetailsService(
        //    IvwEmployeeDetailsService repository,
        //    ILogger<VwEmployeeDetailsService> logger)
        //{
        //    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        //    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        //}

        public async Task<VwEmployeeDetails?> GetEmployeeDetailsAsync(
            int employeeId,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting employee details for ID: {EmployeeId}", employeeId);

                if (employeeId <= 0)
                {
                    _logger.LogWarning("Invalid employee ID provided: {EmployeeId}", employeeId);
                    return null;
                }

                var result = await _repository.GetByIdAsync(employeeId, ct);

                if (result == null)
                {
                    _logger.LogInformation("Employee not found with ID: {EmployeeId}", employeeId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee details for ID: {EmployeeId}", employeeId);
                throw;
            }
        }

        //public async Task<PagedResultDto<EmployeeDetailsDto>> GetEmployeeDetailsPagedAsync(
        //    EmployeeDetailsFilterDto filter,
        //    CancellationToken ct = default)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Getting paged employee details with filter");

        //        ValidateFilter(filter);

        //        return await _repository.GetPagedAsync(filter, ct);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting paged employee details");
        //        throw;
        //    }
        //}

        public async Task<IEnumerable<VwEmployeeDetails>> GetAllEmployeeDetailsAsync(
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting all employee details");
                return await _repository.GetAllAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all employee details");
                throw;
            }
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetEmployeesByDepartmentAsync(
            string departmentName,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting employees by department: {Department}", departmentName);

                if (string.IsNullOrWhiteSpace(departmentName))
                {
                    throw new ArgumentException("Department name cannot be null or empty", nameof(departmentName));
                }

                return await _repository.GetByDepartmentAsync(departmentName, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees by department: {Department}", departmentName);
                throw;
            }
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetEmployeesByFacultyAsync(
            string facultyName,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting employees by faculty: {Faculty}", facultyName);

                if (string.IsNullOrWhiteSpace(facultyName))
                {
                    throw new ArgumentException("Faculty name cannot be null or empty", nameof(facultyName));
                }

                return await _repository.GetByFacultyAsync(facultyName, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees by faculty: {Faculty}", facultyName);
                throw;
            }
        }

        public async Task<IEnumerable<VwEmployeeDetails>> GetEmployeesByTypeAsync(
            int employeeType,
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting employees by type: {EmployeeType}", employeeType);

                //if (string.IsNullOrWhiteSpace(employeeType))
                if (employeeType <= 0)
                {
                    throw new ArgumentException("Employee type cannot be null or empty", nameof(employeeType));
                }

                return await _repository.GetByEmployeeTypeAsync(employeeType, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employees by type: {EmployeeType}", employeeType);
                throw;
            }
        }

        public async Task<IEnumerable<int>> GetAvailableEmployeeTypesAsync(
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting available employee types");
                return await _repository.GetEmployeeTypesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available employee types");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableDepartmentsAsync(
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting available departments");
                return await _repository.GetDepartmentsAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available departments");
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAvailableFacultiesAsync(
            CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Getting available faculties");
                return await _repository.GetFacultiesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available faculties");
                throw;
            }
        }

     





        //private static void ValidateFilter(EmployeeDetailsFilterDto filter)
        //{
        //    if (filter.PageNumber < 1)
        //        filter.PageNumber = 1;

        //    if (filter.PageSize < 1 || filter.PageSize > 100)
        //        filter.PageSize = 10;

        //    if (filter.MinSalary.HasValue && filter.MaxSalary.HasValue &&
        //        filter.MinSalary > filter.MaxSalary)
        //    {
        //        throw new ArgumentException("MinSalary cannot be greater than MaxSalary");
        //    }

        //    if (filter.HireDateFrom.HasValue && filter.HireDateTo.HasValue &&
        //        filter.HireDateFrom > filter.HireDateTo)
        //    {
        //        throw new ArgumentException("HireDateFrom cannot be greater than HireDateTo");
        //    }
        //}
    }
}

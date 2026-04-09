// VwEmployeeDetailsService.cs
using System.Diagnostics;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Repositories;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Services;

public class VwEmployeeDetailsService : IvwEmployeeDetailsService
{
    private readonly IvwEmployeeDetailsRepository _repository;
    private readonly ILogger<VwEmployeeDetailsService> _logger;

    public VwEmployeeDetailsService(
        IvwEmployeeDetailsRepository repository,
        ILogger<VwEmployeeDetailsService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VwEmployeeDetails?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var normalized = (email ?? string.Empty).Trim();

        try
        {
            if (string.IsNullOrWhiteSpace(normalized))
            {
                //_logger.LogWarning("[EMP-SVC] GetByEmailAsync called with empty email");
                return null;
            }

            //_logger.LogInformation("[EMP-SVC] GetByEmailAsync START email={Email}", normalized);

            var result = await _repository.GetByEmailAsync(normalized, ct);

            //if (result == null)
            //{
            //    _logger.LogInformation("[EMP-SVC] GetByEmailAsync NOT FOUND email={Email} in {Elapsed}ms",
            //        normalized, sw.ElapsedMilliseconds);
            //}
            //else
            //{
            //    _logger.LogInformation("[EMP-SVC] GetByEmailAsync FOUND employeeId={EmployeeId} in {Elapsed}ms",
            //        result.EmployeeID, sw.ElapsedMilliseconds);
            //}
            _logger.LogInformation("");
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("[EMP-SVC] GetByEmailAsync CANCELED email={Email} after {Elapsed}ms",
                normalized, sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMP-SVC] GetByEmailAsync ERROR email={Email} after {Elapsed}ms",
                normalized, sw.ElapsedMilliseconds);
            throw;
        }


    }

    public async Task<VwEmployeeDetails?> GetEmployeeDetailsAsync(int employeeId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            //_logger.LogInformation("[EMP-SVC] GetEmployeeDetailsAsync START employeeId={EmployeeId}", employeeId);

            if (employeeId <= 0)
            {
                //_logger.LogWarning("[EMP-SVC] Invalid employeeId={EmployeeId}", employeeId);
                return null;
            }

            var result = await _repository.GetByIdAsync(employeeId, ct);

            //if (result == null)
            //{
            //    _logger.LogInformation("[EMP-SVC] GetEmployeeDetailsAsync NOT FOUND employeeId={EmployeeId} in {Elapsed}ms",
            //        employeeId, sw.ElapsedMilliseconds);
            //}
            //else
            //{
            //    _logger.LogInformation("[EMP-SVC] GetEmployeeDetailsAsync FOUND employeeId={EmployeeId} in {Elapsed}ms",
            //        employeeId, sw.ElapsedMilliseconds);
            //}

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("[EMP-SVC] GetEmployeeDetailsAsync CANCELED employeeId={EmployeeId} after {Elapsed}ms",
                employeeId, sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMP-SVC] GetEmployeeDetailsAsync ERROR employeeId={EmployeeId} after {Elapsed}ms",
                employeeId, sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<IEnumerable<VwEmployeeDetails>> GetAllEmployeeDetailsAsync(CancellationToken ct = default)
    {
        try
        {
            //_logger.LogInformation("[EMP-SVC] GetAllEmployeeDetailsAsync");
            return await _repository.GetAllAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMP-SVC] Error getting all employee details");
            throw;
        }
    }

    public async Task<IEnumerable<VwEmployeeDetails>> GetEmployeesByDepartmentAsync(string departmentName, CancellationToken ct = default)
    {
        try
        {
            //_logger.LogInformation("[EMP-SVC] GetEmployeesByDepartmentAsync dept={Department}", departmentName);

            if (string.IsNullOrWhiteSpace(departmentName))
                throw new ArgumentException("Department name cannot be null or empty", nameof(departmentName));

            return await _repository.GetByDepartmentAsync(departmentName, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMP-SVC] Error getting employees by department: {Department}", departmentName);
            throw;
        }
    }

    public async Task<IEnumerable<VwEmployeeDetails>> GetEmployeesByFacultyAsync(string facultyName, CancellationToken ct = default)
    {
        try
        {
            //_logger.LogInformation("[EMP-SVC] GetEmployeesByFacultyAsync faculty={Faculty}", facultyName);

            if (string.IsNullOrWhiteSpace(facultyName))
                throw new ArgumentException("Faculty name cannot be null or empty", nameof(facultyName));

            return await _repository.GetByFacultyAsync(facultyName, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMP-SVC] Error getting employees by faculty: {Faculty}", facultyName);
            throw;
        }
    }

    public async Task<IEnumerable<VwEmployeeDetails>> GetEmployeesByTypeAsync(int employeeType, CancellationToken ct = default)
    {
        try
        {
            //_logger.LogInformation("[EMP-SVC] GetEmployeesByTypeAsync type={EmployeeType}", employeeType);

            if (employeeType <= 0)
                throw new ArgumentException("Employee type cannot be null or empty", nameof(employeeType));

            return await _repository.GetByEmployeeTypeAsync(employeeType, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMP-SVC] Error getting employees by type: {EmployeeType}", employeeType);
            throw;
        }
    }

    public async Task<IEnumerable<int>> GetAvailableEmployeeTypesAsync(CancellationToken ct = default)
    {
        try
        {
            //_logger.LogInformation("[EMP-SVC] GetAvailableEmployeeTypesAsync");
            return await _repository.GetEmployeeTypesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMP-SVC] Error getting available employee types");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetAvailableDepartmentsAsync(CancellationToken ct = default)
    {
        try
        {
            //_logger.LogInformation("[EMP-SVC] GetAvailableDepartmentsAsync");
            return await _repository.GetDepartmentsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMP-SVC] GetAvailableDepartmentsAsync error");
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetAvailableFacultiesAsync(CancellationToken ct = default)
    {
        try
        {
            //_logger.LogInformation("[EMP-SVC] GetAvailableFacultiesAsync");
            return await _repository.GetFacultiesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMP-SVC] GetAvailableFacultiesAsync error");
            throw;
        }
    }
    public async Task<PagedResult<VwEmployeeDetails>> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return await _repository.GetPagedAsync(page, pageSize, ct);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<VwEmployeeDetails>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        return await _repository.GetPagedAsync(search, page, pageSize, ct);
    }

    public async Task<IEnumerable<VwEmployeeDetails>> GetSubordinatesByBossIdAsync(
        int bossId,
        CancellationToken ct = default)
    {
        return await _repository.GetByImmediateBossIdAsync(bossId, ct);
    }
}

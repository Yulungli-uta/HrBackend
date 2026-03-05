using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Diagnostics;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR
{
    [ApiController]
    [Route("vw/EmployeeDetails")]
    public class VwEmployeeDetailsController : ControllerBase
    {
        private readonly IvwEmployeeDetailsService _employeeDetailsService;
        private readonly ILogger<VwEmployeeDetailsController> _logger;

        public VwEmployeeDetailsController(IvwEmployeeDetailsService employeeDetailsService, ILogger<VwEmployeeDetailsController> logger)
        {
            _employeeDetailsService = employeeDetailsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            var employees = await _employeeDetailsService.GetAllEmployeeDetailsAsync(ct);
            
            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var employee = await _employeeDetailsService.GetEmployeeDetailsAsync(id, ct);
            return employee != null ? Ok(employee) : NotFound();
        }

        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetByEmail(string email, CancellationToken ct = default)
        {
            //var employee = await _employeeDetailsService.GetByEmailAsync(email, ct);
            //return employee != null ? Ok(employee) : NotFound();

            var sw = Stopwatch.StartNew();
            //_logger.LogInformation("[EMP-CTRL] START GetByEmail email={Email} trace={TraceId}",
            //    email, HttpContext.TraceIdentifier);

            try
            {
                //_logger.LogInformation("CTRL BEFORE service");
                var employee = await _employeeDetailsService.GetByEmailAsync(email, ct);

                //_logger.LogInformation("[EMP-CTRL] END GetByEmail found={Found} status={Status} elapsed={Elapsed}ms trace={TraceId}",
                //    employee != null, employee != null ? 200 : 404, sw.ElapsedMilliseconds, HttpContext.TraceIdentifier);

                return employee != null ? Ok(employee) : NotFound();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("[EMP-CTRL] CANCELED GetByEmail email={Email} elapsed={Elapsed}ms trace={TraceId}",
                    email, sw.ElapsedMilliseconds, HttpContext.TraceIdentifier);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EMP-CTRL] ERROR GetByEmail email={Email} elapsed={Elapsed}ms trace={TraceId}",
                    email, sw.ElapsedMilliseconds, HttpContext.TraceIdentifier);
                throw;
            }
        }

        [HttpGet("department/{departmentName}")]
        public async Task<IActionResult> GetByDepartment(string departmentName, CancellationToken ct = default)
        {
            var employees = await _employeeDetailsService.GetEmployeesByDepartmentAsync(departmentName, ct);
            return Ok(employees);
        }

        [HttpGet("faculty/{facultyName}")]
        public async Task<IActionResult> GetByFaculty(string facultyName, CancellationToken ct = default)
        {
            var employees = await _employeeDetailsService.GetEmployeesByFacultyAsync(facultyName, ct);
            return Ok(employees);
        }

        [HttpGet("type/{employeeType}")]
        public async Task<IActionResult> GetByType(int employeeType, CancellationToken ct = default)
        {
            var employees = await _employeeDetailsService.GetEmployeesByTypeAsync(employeeType, ct);
            return Ok(employees);
        }

        [HttpGet("available/types")]
        public async Task<IActionResult> GetAvailableTypes(CancellationToken ct = default)
        {
            var types = await _employeeDetailsService.GetAvailableEmployeeTypesAsync(ct);
            return Ok(types);
        }

        [HttpGet("available/departments")]
        public async Task<IActionResult> GetAvailableDepartments(CancellationToken ct = default)
        {
            var departments = await _employeeDetailsService.GetAvailableDepartmentsAsync(ct);
            return Ok(departments);
        }

        [HttpGet("available/faculties")]
        public async Task<IActionResult> GetAvailableFaculties(CancellationToken ct = default)
        {
            var faculties = await _employeeDetailsService.GetAvailableFacultiesAsync(ct);
            return Ok(faculties);
        }
    }
}

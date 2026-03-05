using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR
{
    [ApiController]
    [Route("vw/EmployeeDetails")]
    public class VwEmployeeDetailsController : ControllerBase
    {
        private readonly IvwEmployeeDetailsService _employeeDetailsService;
        private readonly ILogger<VwEmployeeDetailsController> _logger;

        public VwEmployeeDetailsController(
            IvwEmployeeDetailsService employeeDetailsService,
            ILogger<VwEmployeeDetailsController> logger)
        {
            _employeeDetailsService = employeeDetailsService;
            _logger = logger;
        }

        /// <summary>Lista todos los empleados.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            var employees = await _employeeDetailsService.GetAllEmployeeDetailsAsync(ct);
            return Ok(employees);
        }

        /// <summary>Retorna un resultado paginado de empleados.</summary>
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = "asc",
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var paged = await _employeeDetailsService.GetPagedAsync(page, pageSize, ct);
            return Ok(paged);
        }

        /// <summary>Obtiene un empleado por ID.</summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var employee = await _employeeDetailsService.GetEmployeeDetailsAsync(id, ct);
            return employee != null ? Ok(employee) : NotFound();
        }

        /// <summary>Obtiene un empleado por email.</summary>
        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetByEmail(string email, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var employee = await _employeeDetailsService.GetByEmailAsync(email, ct);
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

        /// <summary>Obtiene empleados por departamento.</summary>
        [HttpGet("department/{departmentName}")]
        public async Task<IActionResult> GetByDepartment(string departmentName, CancellationToken ct = default)
        {
            var employees = await _employeeDetailsService.GetEmployeesByDepartmentAsync(departmentName, ct);
            return Ok(employees);
        }

        /// <summary>Obtiene empleados por facultad.</summary>
        [HttpGet("faculty/{facultyName}")]
        public async Task<IActionResult> GetByFaculty(string facultyName, CancellationToken ct = default)
        {
            var employees = await _employeeDetailsService.GetEmployeesByFacultyAsync(facultyName, ct);
            return Ok(employees);
        }

        /// <summary>Obtiene empleados por tipo.</summary>
        [HttpGet("type/{employeeType}")]
        public async Task<IActionResult> GetByType(int employeeType, CancellationToken ct = default)
        {
            var employees = await _employeeDetailsService.GetEmployeesByTypeAsync(employeeType, ct);
            return Ok(employees);
        }

        /// <summary>Obtiene los tipos de empleado disponibles.</summary>
        [HttpGet("available/types")]
        public async Task<IActionResult> GetAvailableTypes(CancellationToken ct = default)
        {
            var types = await _employeeDetailsService.GetAvailableEmployeeTypesAsync(ct);
            return Ok(types);
        }

        /// <summary>Obtiene los departamentos disponibles.</summary>
        [HttpGet("available/departments")]
        public async Task<IActionResult> GetAvailableDepartments(CancellationToken ct = default)
        {
            var departments = await _employeeDetailsService.GetAvailableDepartmentsAsync(ct);
            return Ok(departments);
        }

        /// <summary>Obtiene las facultades disponibles.</summary>
        [HttpGet("available/faculties")]
        public async Task<IActionResult> GetAvailableFaculties(CancellationToken ct = default)
        {
            var faculties = await _employeeDetailsService.GetAvailableFacultiesAsync(ct);
            return Ok(faculties);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers.HR
{
    [ApiController]
    [Route("vw/EmployeeComplete")]
    public class VwEmployeesController : ControllerBase
    {
        private readonly IvwEmployeeCompleteService _employeeService;

        public VwEmployeesController(IvwEmployeeCompleteService employeeService)
        {
            _employeeService = employeeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            return Ok(employees);
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = "asc",
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 20;

            var paged = !string.IsNullOrWhiteSpace(search)
                ? await _employeeService.GetPagedAsync(search, page, pageSize, ct)
                : await _employeeService.GetPagedAsync(page, pageSize, ct);

            return Ok(paged);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken ct = default)
        {
            var stats = await _employeeService.GetStatsAsync(ct);
            return Ok(stats);
        }

        [HttpGet("stats/by-contract-type")]
        public async Task<IActionResult> GetByContractType(CancellationToken ct = default)
        {
            var result = await _employeeService.GetByContractTypeAsync(ct);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            return employee != null ? Ok(employee) : NotFound();
        }

        [HttpGet("department/{department}")]
        public async Task<IActionResult> GetByDepartment(string department)
        {
            var employees = await _employeeService.GetEmployeesByDepartmentAsync(department);
            return Ok(employees);
        }
    }
}

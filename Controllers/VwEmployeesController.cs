using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Controllers
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

using WsUtaSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IvwEmployeeCompleteService
    {
        Task<IEnumerable<VwEmployeeComplete>> GetAllEmployeesAsync();
        Task<VwEmployeeComplete> GetEmployeeByIdAsync(int employeeId);
        Task<IEnumerable<VwEmployeeComplete>> GetEmployeesByDepartmentAsync(string department);
    }
}

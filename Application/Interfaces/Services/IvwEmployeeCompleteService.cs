using System.Collections.Generic;
using System.Threading.Tasks;
using WsUtaSystem.Models.Views;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IvwEmployeeCompleteService
    {
        Task<IEnumerable<VwEmployeeComplete>> GetAllEmployeesAsync();
        Task<VwEmployeeComplete> GetEmployeeByIdAsync(int employeeId);
        Task<IEnumerable<VwEmployeeComplete>> GetEmployeesByDepartmentAsync(string department);
    }
}

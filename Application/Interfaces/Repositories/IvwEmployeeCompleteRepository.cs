using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IvwEmployeeCompleteRepository
    {
        Task<IEnumerable<VwEmployeeComplete>> GetAllAsync();
        Task<VwEmployeeComplete> GetByIdAsync(int employeeId);
        Task<IEnumerable<VwEmployeeComplete>> GetByDepartmentAsync(string department);
    }
}

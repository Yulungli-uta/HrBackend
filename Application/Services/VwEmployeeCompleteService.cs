using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class VwEmployeeCompleteService : IvwEmployeeCompleteService
    {
        private readonly IvwEmployeeCompleteRepository _repository;

        public VwEmployeeCompleteService(IvwEmployeeCompleteRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<VwEmployeeComplete>> GetAllEmployeesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<VwEmployeeComplete> GetEmployeeByIdAsync(int employeeId)
        {
            return await _repository.GetByIdAsync(employeeId);
        }

        public async Task<IEnumerable<VwEmployeeComplete>> GetEmployeesByDepartmentAsync(string department)
        {
            return await _repository.GetByDepartmentAsync(department);
        }
    }
}

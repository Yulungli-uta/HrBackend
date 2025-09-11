using WsUtaSystem.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WsUtaSystem.Infrastructure.Repositories
{
    public class VwEmployeeCompleteRepository : IvwEmployeeCompleteRepository
    {
        private readonly AppDbContext _context;

        public VwEmployeeCompleteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VwEmployeeComplete>> GetAllAsync()
        {
            return await _context.vwEmployeeComplete
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<VwEmployeeComplete> GetByIdAsync(int employeeId)
        {
            return await _context.vwEmployeeComplete
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeID == employeeId);
        }

        public async Task<IEnumerable<VwEmployeeComplete>> GetByDepartmentAsync(string department)
        {
            return await _context.vwEmployeeComplete
                .AsNoTracking()
                .Where(e => e.Department == department)
                .ToListAsync();
        }
    }
}
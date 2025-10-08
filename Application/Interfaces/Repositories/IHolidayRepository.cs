using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IHolidayRepository : IRepository<Holiday, int>
    {
        Task<IEnumerable<Holiday>> GetByYearAsync(int year, CancellationToken ct = default);
        Task<IEnumerable<Holiday>> GetActiveHolidaysAsync(CancellationToken ct = default);
        Task<bool> ExistsByDateAsync(DateTime holidayDate, int? excludeHolidayId = null, CancellationToken ct = default);
        Task<bool> IsHolidayAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<DateTime>> GetHolidayDatesBetweenAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    }
}

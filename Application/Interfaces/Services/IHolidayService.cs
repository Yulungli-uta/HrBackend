using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface IHolidayService : IService<Holiday, int>
    {
        Task<IEnumerable<Holiday>> GetByYearAsync(int year, CancellationToken ct = default);
        Task<IEnumerable<Holiday>> GetActiveHolidaysAsync(CancellationToken ct = default);
        Task<bool> IsHolidayAsync(DateTime date, CancellationToken ct = default);
        Task<IEnumerable<DateTime>> GetHolidayDatesBetweenAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
        //Task<PagedResultDTO<Holiday>> SearchHolidaysAsync(HolidaySearchDTO searchDto, CancellationToken ct = default);
    }
}

using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class HolidayService : Service<Holiday, int>, IHolidayService
    {
        private readonly IHolidayRepository _repository;

        public HolidayService(IHolidayRepository repo) : base(repo)
        {
            _repository = repo;
        }

        public async Task<IEnumerable<Holiday>> GetByYearAsync(int year, CancellationToken ct = default)
        {
            return await _repository.GetByYearAsync(year, ct);
        }

        public async Task<IEnumerable<Holiday>> GetActiveHolidaysAsync(CancellationToken ct = default)
        {
            return await _repository.GetActiveHolidaysAsync(ct);
        }

        public async Task<bool> IsHolidayAsync(DateTime date, CancellationToken ct = default)
        {
            return await _repository.IsHolidayAsync(date, ct);
        }

        public async Task<IEnumerable<DateTime>> GetHolidayDatesBetweenAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
        {
            return await _repository.GetHolidayDatesBetweenAsync(startDate, endDate, ct);
        }

        //public async Task<PagedResultDTO<Holiday>> SearchHolidaysAsync(HolidaySearchDTO searchDto, CancellationToken ct = default)
        //{
        //    var query = (await _repository.GetAllAsync(ct)).AsQueryable();

        //    if (searchDto.Year.HasValue)
        //        query = query.Where(h => h.HolidayDate.Year == searchDto.Year.Value);

        //    if (searchDto.IsActive.HasValue)
        //        query = query.Where(h => h.IsActive == searchDto.IsActive.Value);

        //    if (!string.IsNullOrEmpty(searchDto.SearchText))
        //        query = query.Where(h => h.Name.Contains(searchDto.SearchText) ||
        //                                (h.Description != null && h.Description.Contains(searchDto.SearchText)));

        //    var totalCount = query.Count();
        //    var items = query.OrderBy(h => h.HolidayDate)
        //                   .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
        //                   .Take(searchDto.PageSize)
        //                   .ToList();

        //    return new PagedResultDTO<Holiday>
        //    {
        //        Items = items,
        //        TotalCount = totalCount,
        //        PageNumber = searchDto.PageNumber,
        //        PageSize = searchDto.PageSize
        //    };
        //}
    }
}

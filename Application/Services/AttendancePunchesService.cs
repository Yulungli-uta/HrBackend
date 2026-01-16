using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class AttendancePunchesService : Service<AttendancePunches, int>, IAttendancePunchesService
{
    private readonly ILogger<AttendancePunchesService> _logger;
    private readonly IAttendancePunchesRepository _repository;
    public AttendancePunchesService(IAttendancePunchesRepository repo) : base(repo) {

        _repository = repo;
    }

    public async Task<IEnumerable<AttendancePunches>> GetLastPunchAsync(int employeeId, CancellationToken ct)
    {
        return await _repository.GetLastPunchAsync(employeeId, ct);
    }

    public async Task<IEnumerable<AttendancePunches>> GetPunchesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        return await _repository.GetPunchesByDateRangeAsync(startDate, endDate,ct);
    }

    public async Task<IEnumerable<AttendancePunches>> GetPunchesByEmployeeAsync(int employeeId, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        return await _repository.GetPunchesByEmployeeAsync(employeeId, startDate, endDate, ct);
    }

    public async Task<IEnumerable<AttendancePunches>> GetTodayPunchesByEmployeeAsync(int employeeId, CancellationToken ct)
    {
        return await _repository.GetTodayPunchesByEmployeeAsync(employeeId,ct);
        
    }
    public async Task<AttendancePunches> CreatePunchesWithIPAsync(AttendancePunches entity, string ipAddress, CancellationToken ct)
    {
        //var _attendancePunches = attendancePunches;        
        entity.IpAddress = ipAddress;
        return await _repository.CreatePunchWithIPAsync(entity, ct);
    }

}

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IAttendanceCalculationService
{
    Task CalculateRangeAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);
    Task CalculateNightMinutesAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);
}


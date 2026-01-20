namespace WsUtaSystem.Application.Interfaces.Services;

public interface IAttendanceCalculationService
{
    Task CalculateRangeAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);
    Task CalculateNightMinutesAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);
    Task ProcessApplyJustification(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);
    Task ProcessAttendanceRange(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
    Task ProcessApplyOvertimeRecovery(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);
}


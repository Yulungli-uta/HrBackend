namespace WsUtaSystem.Application.Interfaces.Services;

public interface IJustificationsService
{
    Task ApplyJustificationsAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);
}


namespace WsUtaSystem.Application.Interfaces.Services;

public interface IRecoveryService
{
    Task ApplyRecoveryAsync(DateTime fromDate, DateTime toDate, int? employeeId = null, CancellationToken ct = default);
}


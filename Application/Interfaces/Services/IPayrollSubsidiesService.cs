namespace WsUtaSystem.Application.Interfaces.Services;

public interface IPayrollSubsidiesService
{
    Task CalculateSubsidiesAsync(string period, CancellationToken ct = default);
}


namespace WsUtaSystem.Application.Interfaces.Services;

public interface IOvertimePriceService
{
    Task CalculateOvertimePriceAsync(string period, CancellationToken ct = default);
}


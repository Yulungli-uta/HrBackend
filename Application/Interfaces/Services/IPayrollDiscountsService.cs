namespace WsUtaSystem.Application.Interfaces.Services;

public interface IPayrollDiscountsService
{
    Task CalculateDiscountsAsync(string period, CancellationToken ct = default);
}


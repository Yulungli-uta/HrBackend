using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.Contracts;
using WsUtaSystem.Application.DTOs.ContractStatusHistory;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IContractsService : IService<Contracts, int> {
    Task<Contracts> CreateAndNotifyAsync(Contracts entity, CancellationToken ct);
    Task UpdateAndNotifyAsync(int id, Contracts entity, CancellationToken ct);
    Task UpdateAsync(int id, ContractsUpdateDto dto, CancellationToken ct);

    Task<IReadOnlyList<int>> GetAllowedNextStatusesAsync(int currentStatusTypeId, CancellationToken ct);

    Task ChangeStatusAsync(int contractId, int toStatusTypeId, string? comment, CancellationToken ct);

    Task<IReadOnlyList<ContractStatusHistoryDto>> GetStatusHistoryAsync(int contractId, CancellationToken ct);

    Task<IReadOnlyList<Contracts>> GetAddendumsAsync(int contractId, CancellationToken ct);

}

using WsUtaSystem.Application.DTOs.ContractRequest;
using WsUtaSystem.Application.DTOs.ContractRequestPerson;
using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IContractRequestService : IService<ContractRequest, int>
{
    Task<IEnumerable<ContractRequestDto>> GetByStatusAsync(string statusName, CancellationToken ct = default);
    Task<PagedContractRequestResult> GetPagedAsync(ContractRequestQueryFilter filter, CancellationToken ct = default);
    Task<int> GetPendingCountAsync(int requestId, CancellationToken ct = default);
    Task IncrementTotalHiredAsync(int requestId, CancellationToken ct = default);

    /// <summary>Retorna información de cupos (alias que delega al ContractRequestPersonService).</summary>
    Task<ContractRequestSlotsDto> GetSlotsAsync(int requestId, CancellationToken ct = default);

    /// <summary>Busca personas disponibles (no contratadas actualmente) para vincular a una solicitud.</summary>
    Task<IEnumerable<AvailablePersonDto>> SearchAvailablePeopleAsync(int requestId, string? search, CancellationToken ct = default);

    /// <summary>Envía la solicitud a estado PENDIENTE_CORRECCION con la razón indicada.</summary>
    Task SendToCorrectionAsync(int requestId, string reason, int userId, CancellationToken ct = default);
}

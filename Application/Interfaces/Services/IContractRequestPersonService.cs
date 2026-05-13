using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.ContractRequestPerson;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services;

public interface IContractRequestPersonService : IService<ContractRequestPerson, int>
{
    /// <summary>Retorna todas las personas registradas en una solicitud.</summary>
    Task<IEnumerable<ContractRequestPersonDto>> GetByRequestAsync(int requestId, CancellationToken ct = default);

    /// <summary>Retorna solo las personas con estado PENDIENTE de una solicitud.</summary>
    Task<IEnumerable<ContractRequestPersonDto>> GetPendingByRequestAsync(int requestId, CancellationToken ct = default);

    /// <summary>Retorna información de cupos de una solicitud (contratados, libres, pendientes).</summary>
    Task<ContractRequestSlotsDto> GetSlotsAsync(int requestId, CancellationToken ct = default);

    /// <summary>Agrega una persona al detalle de una solicitud.</summary>
    Task<ContractRequestPersonDto> AddPersonAsync(int requestId, CreateContractRequestPersonDto dto, int createdBy, CancellationToken ct = default);

    /// <summary>Actualiza los datos de una persona en el detalle.</summary>
    Task UpdatePersonAsync(int requestPersonId, UpdateContractRequestPersonDto dto, int updatedBy, CancellationToken ct = default);

    /// <summary>Marca una persona como contratada y registra el contractId.</summary>
    Task HireAsync(int requestPersonId, int contractId, int updatedBy, CancellationToken ct = default);

    /// <summary>Registra la contratación de una persona que no estaba en el detalle (desde la lista general).</summary>
    Task RecordHiredFromAvailableAsync(int requestId, int personId, int jobId, int contractId, int createdBy, CancellationToken ct = default);

    /// <summary>Inactiva todos los registros vinculados a un contrato (cuando el contrato es anulado).</summary>
    Task InactivateByContractAsync(int contractId, int updatedBy, CancellationToken ct = default);
}

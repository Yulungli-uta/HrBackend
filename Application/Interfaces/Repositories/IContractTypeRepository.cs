using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IContractTypeRepository : IRepository<ContractType, int>
{
    /// <summary>Obtiene un tipo de contrato con la info de su plantilla por defecto.</summary>
    Task<ContractType?> GetWithDefaultTemplateAsync(int contractTypeId, CancellationToken ct = default);

    /// <summary>Asigna o quita la plantilla por defecto de un tipo de contrato.</summary>
    Task SetDefaultTemplateAsync(int contractTypeId, int? templateId, CancellationToken ct = default);

    /// <summary>Asigna o quita la plantilla de delegación de un tipo de contrato.</summary>
    Task SetDelegationTemplateAsync(int contractTypeId, int? templateId, CancellationToken ct = default);

    /// <summary>
    /// Consume el siguiente número de secuencia para el año dado.
    /// Usa una transacción serializable para evitar duplicados concurrentes.
    /// </summary>
    Task<(string DocumentNumber, int Year, int Sequence)> ConsumeNextNumberAsync(
        int contractTypeId,
        int year,
        CancellationToken ct = default);
}

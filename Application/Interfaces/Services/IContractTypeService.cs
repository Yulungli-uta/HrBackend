using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.DTOs.ContractType;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Interfaces.Services;
public interface IContractTypeService : IService<ContractType, int>
{
    /// <summary>Asigna o quita la plantilla por defecto de un tipo de contrato.</summary>
    Task SetDefaultTemplateAsync(int contractTypeId, int? templateId, CancellationToken ct = default);

    /// <summary>Obtiene un tipo de contrato con la info de su plantilla por defecto.</summary>
    Task<ContractTypeWithTemplateDto?> GetWithDefaultTemplateAsync(int contractTypeId, CancellationToken ct = default);

    /// <summary>
    /// Genera y reserva el siguiente número de documento para el tipo dado.
    /// El número tiene el formato {prefix}-{year}-{seq:D3} (ej: CONT-OCAS-2026-001).
    /// </summary>
    Task<ContractNextNumberDto> GetNextNumberAsync(int contractTypeId, CancellationToken ct = default);
}

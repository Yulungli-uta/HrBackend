using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.SignedFileTracking;

namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Acceso a datos (solo lectura) de las vistas de acciones y contratos con archivo firmado.</summary>
public interface ISignedFileTrackingRepository
{
    /// <summary>Página de acciones de personal con archivo firmado activo.</summary>
    Task<PagedResult<PersonnelActionSignedFileDto>> GetActionsPagedAsync(SignedFileTrackingFilter filter, CancellationToken ct);

    /// <summary>Página de contratos con archivo firmado activo.</summary>
    Task<PagedResult<ContractSignedFileDto>> GetContractsPagedAsync(SignedFileTrackingFilter filter, CancellationToken ct);

    /// <summary>GUID del archivo firmado de una acción, o null si no existe, no tiene archivo activo o queda fuera del alcance.</summary>
    Task<Guid?> GetActionFileGuidAsync(int actionId, IReadOnlyList<int>? allowedDepartmentIds, CancellationToken ct);

    /// <summary>GUID del archivo firmado de un contrato, o null si no existe, no tiene archivo activo o queda fuera del alcance.</summary>
    Task<Guid?> GetContractFileGuidAsync(int contractId, IReadOnlyList<int>? allowedDepartmentIds, CancellationToken ct);
}

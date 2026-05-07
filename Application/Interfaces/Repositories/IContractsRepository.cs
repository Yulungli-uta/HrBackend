using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;
public interface IContractsRepository : IRepository<Contracts, int>
{
    /// <summary>Obtiene un contrato con los campos del documento asociado.</summary>
    Task<Contracts?> GetWithDocumentInfoAsync(int contractId, CancellationToken ct = default);

    /// <summary>Vincula un documento generado al contrato y lo marca como congelado.</summary>
    Task FreezeDocumentAsync(int contractId, int documentId, int templateVersion, CancellationToken ct = default);

    /// <summary>Descongela el documento de un contrato para permitir regeneración.</summary>
    Task UnfreezeDocumentAsync(int contractId, CancellationToken ct = default);

    /// <summary>Cuenta contratos raíz (ParentID IS NULL) asociados a una certificación.</summary>
    Task<int> CountRootContractsByCertificationAsync(int certificationId, CancellationToken ct = default);
}

using WsUtaSystem.Models.Docflow;

namespace WsUtaSystem.Application.Interfaces.Repositories
{
    public interface IDocflowDocumentRepository
    {
        Task<List<DocflowDocument>> GetVisibleByInstanceAsync(Guid instanceId, int currentUserDepartmentId, CancellationToken ct);
        Task<DocflowDocument?> GetByIdAsync(Guid documentId, CancellationToken ct);
        Task CreateAsync(DocflowDocument doc, CancellationToken ct);
        Task UpdateAsync(DocflowDocument doc, CancellationToken ct);

        Task<List<DocflowFileVersion>> GetVersionsAsync(Guid documentId, CancellationToken ct);
        Task<DocflowFileVersion?> GetVersionByIdAsync(Guid versionId, CancellationToken ct);
        Task CreateVersionAsync(DocflowFileVersion version, CancellationToken ct);
    }
}

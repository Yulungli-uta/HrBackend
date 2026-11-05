using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class PublicationsService : Service<Publications, int>, IPublicationsService
{
    private const string DirectoryCode = "HR_HOJA_DE_VIDA";
    private const string EntityTypeName = "PUBLICATION";

    private readonly IPublicationsRepository _repository;
    private readonly IFileManagementService _fileManagement;
    private readonly TransactionalDocumentRepository _txRepo;
    private readonly IPeopleService _people;
    private readonly IScientificProviderCatalog _providers;

    public PublicationsService(
        IPublicationsRepository repo,
        IFileManagementService fileManagement,
        TransactionalDocumentRepository txRepo,
        IPeopleService people,
        IScientificProviderCatalog providers) : base(repo)
    {
        _repository = repo;
        _fileManagement = fileManagement;
        _txRepo = txRepo;
        _people = people;
        _providers = providers;
    }

    /// <summary>
    /// Lo que se registra por la API de la Hoja de Vida es siempre manual y sin revisión: el origen, el estado y los datos de
    /// sincronización nunca los define el cliente (solo la sincronización y la Dirección de Investigación los cambian).
    /// </summary>
    private async Task PrepareManualAsync(Publications entity, CancellationToken ct)
    {
        var manual = await _providers.GetRequiredAsync(ScientificProviderCodes.Manual, ct);
        entity.SourceProviderTypeId = manual.TypeId;
        entity.ExternalId = null;
        entity.DoiNormalized = null;
        entity.ApprovalStatus = PublicationApprovalStatus.SinRevision;
        entity.ApprovedAt = null;
        entity.ApprovedBy = null;
        entity.ReviewNotes = null;
        entity.LastSyncedAt = null;
    }

    public override async Task<Publications> CreateAsync(Publications entity, CancellationToken ct)
    {
        await PrepareManualAsync(entity, ct);
        return await base.CreateAsync(entity, ct);
    }

    public override async Task UpdateAsync(int id, Publications entity, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Publications con clave [{id}] no encontrado para actualización.");

        // Aprobada (congelada) o rechazada: no se modifica. El formulario de edición tampoco envía el origen ni el estado:
        // sin conservarlos, la actualización genérica (SetValues) los reiniciaría.
        PublicationRules.EnsureEditable(existing, "modificar");
        PublicationRules.PreserveProtectedColumns(entity, existing);

        await base.UpdateAsync(id, entity, ct);
    }

    public override async Task DeleteAsync(int id, CancellationToken ct)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is not null) PublicationRules.EnsureEditable(existing, "eliminar");

        await base.DeleteAsync(id, ct);
    }

    public async Task<IReadOnlyList<Publications>> GetCountableByPersonIdAsync(int personId, CancellationToken ct)
    {
        var all = (await _repository.GetByPersonIdAsync(personId)).ToList();
        var providers = await _providers.GetAllAsync(ct);
        return all
            .Where(p => PublicationRules.CountsForCv(p.ApprovalStatus, PublicationRules.RequiresApproval(p, providers)))
            .ToList();
    }

    public async Task<IEnumerable<Publications>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }

    public async Task<(Publications entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        Publications entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct)
    {
        await PrepareManualAsync(entity, ct);
        FileUploadResponseDto? physical = null;

        if (file != null && file.Length > 0)
        {
            var person = await _people.GetByIdAsync(entity.PersonId, ct);
            var idCard = person?.IdCard ?? entity.PersonId.ToString();

            physical = await _fileManagement.UploadFileAsync(new FileUploadRequestDto
            {
                DirectoryCode = DirectoryCode,
                RelativePath = $"{idCard}/{EntityTypeName.ToLowerInvariant()}",
                FileName = file.FileName,
                File = file
            }, ct);

            if (!physical.Success)
            {
                return (null!, null, physical.Message ?? "No se pudo subir el archivo.");
            }
        }

        try
        {
            var (created, storedFile) = await _txRepo.CreateWithDocumentAsync(
                entity,
                physical != null
                    ? (e => StoredFileBuilder.Build(DirectoryCode, EntityTypeName, e.PublicationId.ToString(), file!, physical, documentTypeId))
                    : null,
                ct);

            return (created, storedFile, null);
        }
        catch (Exception)
        {
            if (physical != null)
            {
                await _fileManagement.DeleteFileAsync(DirectoryCode, physical.RelativePath, ct);
            }
            return (null!, null, "No se pudo registrar en la base de datos. Se revirtió el archivo subido.");
        }
    }
}

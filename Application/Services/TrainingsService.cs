using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class TrainingsService : Service<Trainings, int>, ITrainingsService
{
    private const string DirectoryCode = "HR_HOJA_DE_VIDA";
    private const string EntityTypeName = "TRAINING";

    private readonly ITrainingsRepository _repository;
    private readonly IFileManagementService _fileManagement;
    private readonly TransactionalDocumentRepository _txRepo;
    private readonly IPeopleService _people;

    public TrainingsService(
        ITrainingsRepository repo,
        IFileManagementService fileManagement,
        TransactionalDocumentRepository txRepo,
        IPeopleService people) : base(repo)
    {
        _repository = repo;
        _fileManagement = fileManagement;
        _txRepo = txRepo;
        _people = people;
    }

    public async Task<IEnumerable<Trainings>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }

    public async Task<(Trainings entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        Trainings entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct)
    {
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
                    ? (e => StoredFileBuilder.Build(DirectoryCode, EntityTypeName, e.TrainingId.ToString(), file!, physical, documentTypeId))
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

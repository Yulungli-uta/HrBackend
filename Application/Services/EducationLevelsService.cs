using Microsoft.AspNetCore.Http;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class EducationLevelsService : Service<EducationLevels, int>, IEducationLevelsService
{
    // Un solo directorio compartido para toda la hoja de vida (HR_HOJA_DE_VIDA) — se
    // diferencia por identificación de la persona + tipo de entidad en el RelativePath,
    // para tener el expediente completo de cada persona agrupado en una sola carpeta.
    private const string DirectoryCode = "HR_HOJA_DE_VIDA";
    private const string EntityTypeName = "EDUCATION_LEVEL";

    private readonly IEducationLevelsRepository _repository;
    private readonly IFileManagementService _fileManagement;
    private readonly TransactionalDocumentRepository _txRepo;
    private readonly IPeopleService _people;

    public EducationLevelsService(
        IEducationLevelsRepository repo,
        IFileManagementService fileManagement,
        TransactionalDocumentRepository txRepo,
        IPeopleService people) : base(repo)
    {
        _repository = repo;
        _fileManagement = fileManagement;
        _txRepo = txRepo;
        _people = people;
    }

    public async Task<IEnumerable<EducationLevels>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }

    public async Task<(EducationLevels entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        EducationLevels entity,
        IFormFile? file,
        int? documentTypeId,
        CancellationToken ct)
    {
        FileUploadResponseDto? physical = null;

        // 1) Subir el archivo físico PRIMERO (recurso no transaccional — vive en el NAS).
        //    Se agrupa por identificación de la persona para tener todo su expediente junto,
        //    en vez de disperso por tipo de documento.
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

        // 2) INSERT del registro + INSERT de la metadata del archivo, en UNA sola transacción SQL.
        try
        {
            var (created, storedFile) = await _txRepo.CreateWithDocumentAsync(
                entity,
                physical != null
                    ? (e => StoredFileBuilder.Build(DirectoryCode, EntityTypeName, e.EducationId.ToString(), file!, physical, documentTypeId))
                    : null,
                ct);

            return (created, storedFile, null);
        }
        catch (Exception)
        {
            // 3) La transacción SQL falló: revertir (borrar) el archivo físico ya subido.
            if (physical != null)
            {
                await _fileManagement.DeleteFileAsync(DirectoryCode, physical.RelativePath, ct);
            }
            return (null!, null, "No se pudo registrar en la base de datos. Se revirtió el archivo subido.");
        }
    }
}

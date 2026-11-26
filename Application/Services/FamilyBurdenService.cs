using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.FamilyBurden;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class FamilyBurdenService : Service<FamilyBurden, int>, IFamilyBurdenService
{
    private const string DirectoryCode = "HR_HOJA_DE_VIDA";
    private const string EntityTypeName = "FAMILY_MEMBER";
    private const string ModuleTypeCategory = "ACCESS_MODULE_TYPE";
    private const string ModuleTypeName = "FAMILY_BURDEN";
    private const string StatusCategory = "FAMILY_BURDEN_STATUS";

    private readonly IFamilyBurdenRepository _repository;
    private readonly IFileManagementService _fileManagement;
    private readonly TransactionalDocumentRepository _txRepo;
    private readonly IPeopleService _people;
    private readonly ITramiteRequirementsService _tramiteRequirements;
    private readonly WsUtaSystem.Data.AppDbContext _db;

    private readonly IAppClock _clock;

    public FamilyBurdenService(
        IFamilyBurdenRepository repo,
        IFileManagementService fileManagement,
        TransactionalDocumentRepository txRepo,
        IPeopleService people,
        ITramiteRequirementsService tramiteRequirements,
        WsUtaSystem.Data.AppDbContext db,
        IAppClock clock) : base(repo)
    {
        _clock = clock;
        _repository = repo;
        _fileManagement = fileManagement;
        _txRepo = txRepo;
        _people = people;
        _tramiteRequirements = tramiteRequirements;
        _db = db;
    }

    public async Task<IEnumerable<FamilyBurden>> GetByPersonIdAsync(int personId)
    {
        return await _repository.GetByPersonIdAsync(personId);
    }

    /// <summary>Crea la carga familiar; su discapacidad debe cumplir <see cref="FamilyDisabilityRules"/>.</summary>
    public override async Task<FamilyBurden> CreateAsync(FamilyBurden entity, CancellationToken ct)
    {
        ApplyDisabilityRules(entity);
        return await base.CreateAsync(entity, ct);
    }

    /// <summary>
    /// Actualiza la carga familiar. La discapacidad se valida solo si cambia: las cargas anteriores que ya tenían tipo sin porcentaje
    /// no pueden corregirse desde aquí y no deben impedir guardar otros datos.
    /// </summary>
    public override async Task UpdateAsync(int id, FamilyBurden entity, CancellationToken ct)
    {
        // Se leen los valores antes de actualizar: la entidad leída es la misma instancia que el repositorio modifica al guardar.
        var current = await _repo.GetByIdAsync(id, ct);
        var unchanged = current is not null
            && !FamilyDisabilityRules.HasChanged(current.DisabilityTypeId, current.DisabilityPercentage, entity.DisabilityTypeId, entity.DisabilityPercentage);

        if (!unchanged) ApplyDisabilityRules(entity);
        await base.UpdateAsync(id, entity, ct);
    }

    private static void ApplyDisabilityRules(FamilyBurden entity)
    {
        var (typeId, percentage) = FamilyDisabilityRules.ValidateAndNormalize(entity.DisabilityTypeId, entity.DisabilityPercentage);
        entity.DisabilityTypeId = typeId;
        entity.DisabilityPercentage = percentage;
    }

    public async Task<(FamilyBurden entity, StoredFile? storedFile, string? error)> CreateWithDocumentAsync(
        FamilyBurden entity,
        IFormFile? file,
        int? documentTypeId,
        IFormFile? disabilityFile,
        int? disabilityDocumentTypeId,
        CancellationToken ct)
    {
        // Antes de subir ningún archivo: si la discapacidad no cumple la regla se devuelve el mensaje y no queda nada subido.
        try { ApplyDisabilityRules(entity); }
        catch (BusinessRuleException ex) { return (null!, null, ex.Message); }

        var moduleTypeId = await ResolveRefTypeIdAsync(ModuleTypeCategory, ModuleTypeName, ct);
        if (moduleTypeId.HasValue)
        {
            var requirements = await _tramiteRequirements.GetApplicableAsync(moduleTypeId.Value, null, ct);
            var hasMissingRequiredDocument = requirements.Any(r => r.IsRequired) && (file is null || file.Length == 0);
            if (hasMissingRequiredDocument)
            {
                var faltante = requirements.First(r => r.IsRequired).DocumentTypeName ?? "el documento obligatorio";
                return (null!, null, $"Debe adjuntar {faltante} para registrar la carga familiar.");
            }
        }

        var person = await _people.GetByIdAsync(entity.PersonId, ct);
        var idCard = person?.IdCard ?? entity.PersonId.ToString();

        FileUploadResponseDto? physical = null;
        if (file != null && file.Length > 0)
        {
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

        entity.StatusTypeId = await ResolveRefTypeIdAsync(StatusCategory, "REGISTRADO", ct);

        FamilyBurden created;
        StoredFile? storedFile;
        try
        {
            (created, storedFile) = await _txRepo.CreateWithDocumentAsync(
                entity,
                physical != null
                    ? (e => StoredFileBuilder.Build(DirectoryCode, EntityTypeName, e.BurdenId.ToString(), file!, physical, documentTypeId))
                    : null,
                ct);
        }
        catch (Exception)
        {
            if (physical != null)
            {
                await _fileManagement.DeleteFileAsync(DirectoryCode, physical.RelativePath, ct);
            }
            return (null!, null, "No se pudo registrar en la base de datos. Se revirtió el archivo subido.");
        }

        // Certificado de discapacidad: opcional, se adjunta como un segundo documento
        // independiente ya con el BurdenId real. No es parte de la misma transacción que
        // el registro (ese ya quedó confirmado arriba) — si esta subida falla, el registro
        // y el primer documento (obligatorio) igual quedan creados; el usuario puede
        // adjuntar el certificado de discapacidad después desde el expediente.
        if (disabilityFile != null && disabilityFile.Length > 0)
        {
            var disabilityPhysical = await _fileManagement.UploadFileAsync(new FileUploadRequestDto
            {
                DirectoryCode = DirectoryCode,
                RelativePath = $"{idCard}/{EntityTypeName.ToLowerInvariant()}",
                FileName = disabilityFile.FileName,
                File = disabilityFile
            }, ct);

            if (disabilityPhysical.Success)
            {
                var disabilityStoredFile = StoredFileBuilder.Build(
                    DirectoryCode, EntityTypeName, created.BurdenId.ToString(), disabilityFile, disabilityPhysical, disabilityDocumentTypeId);
                _db.Set<StoredFile>().Add(disabilityStoredFile);
                await _db.SaveChangesAsync(ct);
            }
        }

        return (created, storedFile, null);
    }

    public async Task<PagedResult<FamilyBurdenValidationListItemDto>> GetForValidationAsync(
        int? statusTypeId, string? search, int page, int pageSize, CancellationToken ct)
        => await _repository.GetForValidationAsync(statusTypeId, search, page, pageSize, ct);

    public async Task<FamilyBurdenStatsDto> GetStatsAsync(CancellationToken ct)
        => await _repository.GetStatsAsync(ct);

    public async Task ApproveAsync(int burdenId, int approvedByEmployeeId, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(burdenId, ct)
            ?? throw new KeyNotFoundException($"Carga familiar {burdenId} no encontrada.");

        entity.StatusTypeId = await ResolveRefTypeIdAsync(StatusCategory, "APROBADO", ct);
        entity.ApprovedAt = _clock.Now;
        entity.ApprovedBy = approvedByEmployeeId;
        entity.RejectedAt = null;
        entity.RejectedBy = null;
        entity.RejectionReason = null;

        await _repository.UpdateAsync(burdenId, entity, ct);
    }

    public async Task RejectAsync(int burdenId, int rejectedByEmployeeId, string reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("El motivo de rechazo es obligatorio.", nameof(reason));

        var entity = await _repository.GetByIdAsync(burdenId, ct)
            ?? throw new KeyNotFoundException($"Carga familiar {burdenId} no encontrada.");

        entity.StatusTypeId = await ResolveRefTypeIdAsync(StatusCategory, "RECHAZADO", ct);
        entity.RejectedAt = _clock.Now;
        entity.RejectedBy = rejectedByEmployeeId;
        entity.RejectionReason = reason.Trim();
        entity.ApprovedAt = null;
        entity.ApprovedBy = null;

        await _repository.UpdateAsync(burdenId, entity, ct);
    }

    private async Task<int?> ResolveRefTypeIdAsync(string category, string name, CancellationToken ct)
        => await _db.Set<RefTypes>()
            .Where(r => r.Category == category && r.Name == name)
            .Select(r => (int?)r.TypeId)
            .FirstOrDefaultAsync(ct);
}

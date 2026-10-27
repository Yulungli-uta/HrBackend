using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.AcademicPromotion;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.FileManagement;
using WsUtaSystem.Application.DTOs.ResearchProjects;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Proyectos de investigación: validación de reglas (fechas, etapas, prórroga, coordinador), carga de las
/// resoluciones como archivos del sistema y armado de las vistas (gestión, hoja de vida y escalafón docente).
/// </summary>
public sealed class ResearchProjectsService : IResearchProjectsService
{
    private const string DirectoryCode = "HR_RESEARCH_PROJECTS";
    private const string EntityTypeName = "RESEARCH_PROJECT";
    private const string RoleCategory = "RESEARCH_PROJECT_ROLE";
    private const string RolePrincipal = "COORDINADOR_PRINCIPAL";
    private const long MaxFileBytes = 15L * 1024 * 1024;
    private const int MaxSearchResults = 20;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IResearchProjectsRepository _repository;
    private readonly IFileManagementService _fileManagement;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<ResearchProjectsService> _logger;
    private readonly IAppClock _clock;

    public ResearchProjectsService(
        IResearchProjectsRepository repository,
        IFileManagementService fileManagement,
        IHttpContextAccessor http,
        ILogger<ResearchProjectsService> logger,
        IAppClock clock)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _fileManagement = fileManagement ?? throw new ArgumentNullException(nameof(fileManagement));
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    // ── Consultas ────────────────────────────────────────────────────────────────

    public async Task<PagedResult<ResearchProjectListItemDto>> SearchAsync(
        string? search, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            throw new BusinessRuleException("La fecha \"desde\" no puede ser posterior a la fecha \"hasta\".");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (rows, total) = await _repository.SearchAsync(search, from, to, page, pageSize, ct);
        return new PagedResult<ResearchProjectListItemDto>
        {
            Items = rows.Select(r => new ResearchProjectListItemDto(r.Id, ProjectCode(r.Id), r.Name, r.StartDate, r.EndDate, r.HasExtension, r.ParticipantsCount)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    public async Task<ResearchProjectDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, track: false, ct);
        if (project is null) return null;
        return (await BuildDtosAsync([project], personFilter: null, ct))[0];
    }

    public async Task<IReadOnlyList<ResearchPersonLookupDto>> SearchPeopleAsync(string? term, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 3) return [];
        return await _repository.SearchPeopleAsync(term, MaxSearchResults, ct);
    }

    public async Task<IReadOnlyList<ResearchProjectDto>> GetByPersonAsync(int personId, CancellationToken ct = default)
    {
        var projects = await _repository.GetByPersonAsync(personId, ct);
        return await BuildDtosAsync(projects, personFilter: personId, ct);
    }

    public async Task<IReadOnlyList<ResearchProjectParticipationBlockDto>> GetAcademicBlockAsync(int personId, CancellationToken ct = default)
    {
        var projects = await GetByPersonAsync(personId, ct);
        return projects.Select(p => new ResearchProjectParticipationBlockDto(
            Id: p.Code,
            Name: p.Name,
            StartDate: p.StartDate,
            EndDate: p.EndDate,
            ApprovalResolution: new ResearchResolutionBlockDto(p.ApprovalResolution.Number, p.ApprovalResolution.DocumentUrl),
            CompletionResolution: new ResearchResolutionBlockDto(p.CompletionResolution.Number, p.CompletionResolution.DocumentUrl),
            HasExtension: p.HasExtension,
            Extension: p.Extension is null
                ? null
                : new ResearchExtensionBlockDto(p.Extension.EndDate, p.Extension.ResolutionNumber, p.Extension.DocumentUrl),
            Participations: p.Participations.Select(x => new ResearchParticipationBlockDto(
                x.Code, x.RoleCode, x.StartDate, x.EndDate)).ToList()
        )).ToList();
    }

    public async Task<ResearchDocumentDownload> GetDocumentAsync(Guid fileGuid, bool privileged, int? myPersonId, CancellationToken ct = default)
    {
        var file = await _repository.GetStoredFileByGuidAsync(fileGuid, ct);
        if (file is null || file.EntityType != EntityTypeName) return new(false, false, null, null, null);

        var allowed = privileged
            || (myPersonId.HasValue && await _repository.IsParticipantOfFileAsync(fileGuid, myPersonId.Value, ct));
        if (!allowed) return new(true, false, null, null, null);

        var downloaded = await _fileManagement.DownloadFileAsync(file.DirectoryCode, Path.Combine(file.RelativeFolder, file.StoredFileName), ct);
        if (downloaded is null) return new(false, true, null, null, null);

        return new(true, true, downloaded.Value.fileBytes, file.ContentType ?? downloaded.Value.contentType, file.OriginalFileName ?? downloaded.Value.fileName);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var project = await _repository.GetByIdAsync(id, track: false, ct);
        if (project is null) return false;
        await _repository.SoftDeleteAsync(id, ct);
        return true;
    }

    // ── Registro / edición ───────────────────────────────────────────────────────

    public async Task<ResearchProjectDto> SaveAsync(
        int? id, string? dataJson, IFormFile? approvalFile, IFormFile? extensionFile, IFormFile? completionFile,
        CancellationToken ct = default)
    {
        var dto = ParseData(dataJson);

        var project = id.HasValue
            ? await _repository.GetByIdAsync(id.Value, track: true, ct) ?? throw new KeyNotFoundException("El proyecto de investigación no existe.")
            : new ResearchProject();
        var isNew = !id.HasValue;

        // Los archivos de las resoluciones son OPCIONALES (el número de cada resolución sí es obligatorio);
        // al editar, un archivo nuevo reemplaza al anterior.
        foreach (var (file, label) in new[] { (approvalFile, "aprobación"), (extensionFile, "prórroga"), (completionFile, "finalización") })
            ValidateFile(file, label);

        var refTypes = await _repository.GetRefTypesAsync([RoleCategory], ct);
        ValidateProjectData(dto);
        await ValidateParticipationsAsync(dto, refTypes, ct);

        // 1) Archivos físicos primero (NAS, no transaccional): si la BD falla se borran.
        var uploaded = new Dictionary<ResearchDocumentKind, (StoredFile Stored, FileUploadResponseDto Physical)>();
        try
        {
            var folder = string.Empty; // RelativePath vacío: el archivo queda en {PhysicalPath}/{año}/ y TBL_StoredFile lo vincula al proyecto
            if (approvalFile is not null) uploaded[ResearchDocumentKind.Approval] = await UploadAsync(approvalFile, folder, ct);
            if (dto.HasExtension && extensionFile is not null) uploaded[ResearchDocumentKind.Extension] = await UploadAsync(extensionFile, folder, ct);
            if (completionFile is not null) uploaded[ResearchDocumentKind.Completion] = await UploadAsync(completionFile, folder, ct);

            // 2) Datos del proyecto
            project.Name = dto.Name.Trim();
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;
            project.ApprovalResolutionNumber = dto.ApprovalResolutionNumber.Trim();
            project.CompletionResolutionNumber = dto.CompletionResolutionNumber.Trim();
            project.HasExtension = dto.HasExtension;
            project.ExtensionResolutionNumber = dto.HasExtension ? dto.ExtensionResolutionNumber!.Trim() : null;
            project.ExtensionEndDate = dto.HasExtension ? dto.ExtensionEndDate : null;
            if (!dto.HasExtension) project.ExtensionFileId = null;

            // 3) Participaciones: se actualizan por Id, se agregan las nuevas y se dan de baja las que ya no vienen.
            SyncParticipations(project, dto.Participations);

            await _repository.SaveAsync(project, isNew, uploaded.ToDictionary(k => k.Key, k => k.Value.Stored), ct);
        }
        catch
        {
            foreach (var (_, up) in uploaded)
                await _fileManagement.DeleteFileAsync(DirectoryCode, up.Physical.RelativePath, ct);
            throw;
        }

        _logger.LogInformation("Proyecto de investigación {ProjectId} {Action}.", project.ResearchProjectId, isNew ? "creado" : "actualizado");
        return (await GetAsync(project.ResearchProjectId, ct))!;
    }

    private static ResearchProjectUpsertDto ParseData(string? dataJson)
    {
        if (string.IsNullOrWhiteSpace(dataJson)) throw new BusinessRuleException("Faltan los datos del proyecto.");
        try
        {
            return JsonSerializer.Deserialize<ResearchProjectUpsertDto>(dataJson, JsonOptions)
                   ?? throw new BusinessRuleException("Faltan los datos del proyecto.");
        }
        catch (JsonException)
        {
            throw new BusinessRuleException("Los datos del proyecto no tienen un formato válido.");
        }
    }

    private static void ValidateFile(IFormFile? file, string label)
    {
        if (file is null) return;
        if (file.Length == 0) throw new BusinessRuleException($"El archivo de la resolución de {label} está vacío.");
        if (file.Length > MaxFileBytes) throw new BusinessRuleException($"El archivo de la resolución de {label} supera los 15 MB.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException($"El archivo de la resolución de {label} debe ser un PDF.");
    }

    private void ValidateProjectData(ResearchProjectUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) throw new BusinessRuleException("El nombre del proyecto es obligatorio.");
        if (dto.Name.Trim().Length > 500) throw new BusinessRuleException("El nombre del proyecto no puede superar los 500 caracteres.");
        if (dto.EndDate < dto.StartDate) throw new BusinessRuleException("La fecha de finalización no puede ser anterior a la de inicio.");
        // La resolución de finalización es obligatoria: solo se registran proyectos ya terminados.
        if (dto.EndDate > Today) throw new BusinessRuleException("La fecha de finalización no puede ser posterior a hoy.");

        RequireNumber(dto.ApprovalResolutionNumber, "aprobación");
        RequireNumber(dto.CompletionResolutionNumber, "finalización");

        if (dto.HasExtension)
        {
            RequireNumber(dto.ExtensionResolutionNumber, "prórroga");
            if (!dto.ExtensionEndDate.HasValue) throw new BusinessRuleException("La fecha de fin de la prórroga es obligatoria.");
            if (dto.ExtensionEndDate.Value <= dto.EndDate)
                throw new BusinessRuleException("La fecha de fin de la prórroga debe ser posterior a la fecha de finalización del proyecto.");
            if (dto.ExtensionEndDate.Value > Today)
                throw new BusinessRuleException("La fecha de fin de la prórroga no puede ser posterior a hoy.");
        }

        if (dto.Participations.Count == 0) throw new BusinessRuleException("Debe registrar al menos un participante.");
    }

    /// <summary>Fecha de hoy según el reloj del servidor (hora de Ecuador), sin hora.</summary>
    private DateOnly Today => _clock.Today;

    private static void RequireNumber(string? number, string label)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new BusinessRuleException($"El número de la resolución de {label} es obligatorio.");
        if (number.Trim().Length > 100) throw new BusinessRuleException($"El número de la resolución de {label} no puede superar los 100 caracteres.");
    }

    private async Task ValidateParticipationsAsync(ResearchProjectUpsertDto dto, IReadOnlyList<ResearchRefTypeLite> refTypes, CancellationToken ct)
    {
        var roles = refTypes.Where(r => r.Category == RoleCategory).ToDictionary(r => r.Id);
        var projectLastDay = dto.HasExtension && dto.ExtensionEndDate.HasValue ? dto.ExtensionEndDate.Value : dto.EndDate;

        var internalIds = dto.Participations.Where(p => !p.IsExternal && p.PersonId.HasValue).Select(p => p.PersonId!.Value).ToList();
        var people = await _repository.GetPeopleAsync(internalIds, ct);

        // Una misma persona no puede tener dos participaciones que se crucen en fechas, aunque el rol sea distinto.
        var rangesByPerson = new Dictionary<string, List<(DateOnly Start, DateOnly End, int Number)>>();
        var principalRanges = new List<(DateOnly Start, DateOnly End)>();

        for (var i = 0; i < dto.Participations.Count; i++)
        {
            var p = dto.Participations[i];
            var n = $"Participante {i + 1}: ";

            if (!roles.TryGetValue(p.RoleTypeId, out var role)) throw new BusinessRuleException(n + "el rol no es válido.");
            if (p.EndDate < p.StartDate) throw new BusinessRuleException(n + "la fecha de fin de funciones no puede ser anterior a la de inicio.");
            if (p.EndDate > Today) throw new BusinessRuleException(n + "la fecha de fin de funciones no puede ser posterior a hoy.");

            string identity;
            if (p.IsExternal)
            {
                if (string.IsNullOrWhiteSpace(p.ExternalFullName)) throw new BusinessRuleException(n + "el nombre completo es obligatorio.");
                if (string.IsNullOrWhiteSpace(p.ExternalIdentification)) throw new BusinessRuleException(n + "la identificación es obligatoria.");
                if (p.ExternalFullName.Trim().Length > 300) throw new BusinessRuleException(n + "el nombre no puede superar los 300 caracteres.");
                if (p.ExternalIdentification.Trim().Length > 30) throw new BusinessRuleException(n + "la identificación no puede superar los 30 caracteres.");
                identity = "E:" + p.ExternalIdentification.Trim().ToUpperInvariant();
            }
            else
            {
                if (!p.PersonId.HasValue || !people.ContainsKey(p.PersonId.Value)) throw new BusinessRuleException(n + "la persona seleccionada no existe.");
                identity = "I:" + p.PersonId.Value;
            }

            // Las fechas deben caer dentro del proyecto (más la prórroga, si la hubo).
            if (p.StartDate < dto.StartDate || p.EndDate > projectLastDay)
                throw new BusinessRuleException(n + "las fechas deben estar dentro del período del proyecto (incluida la prórroga, si la hubo).");

            if (!rangesByPerson.TryGetValue(identity, out var personRanges))
            {
                personRanges = new List<(DateOnly, DateOnly, int)>();
                rangesByPerson[identity] = personRanges;
            }
            var clash = personRanges.FirstOrDefault(r => p.StartDate <= r.End && p.EndDate >= r.Start);
            if (clash.Number != 0)
                throw new BusinessRuleException(n + $"la misma persona ya tiene la participación {clash.Number} que se cruza en fechas ({clash.Start:dd/MM/yyyy} al {clash.End:dd/MM/yyyy}); no puede coincidir aunque el rol sea distinto.");
            personRanges.Add((p.StartDate, p.EndDate, i + 1));

            if (role.Name == RolePrincipal) principalRanges.Add((p.StartDate, p.EndDate));
        }

        var ordered = principalRanges.OrderBy(r => r.Start).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Start <= ordered[i - 1].End)
                throw new BusinessRuleException("No puede haber dos Coordinadores Principales al mismo tiempo.");
        }
    }

    private static void SyncParticipations(ResearchProject project, List<ResearchParticipationUpsertDto> incoming)
    {
        var existing = project.Participations.Where(x => !x.IsDeleted).ToDictionary(x => x.ParticipationId);
        var keep = new HashSet<int>();

        foreach (var dto in incoming)
        {
            ResearchProjectParticipation entity;
            if (dto.Id.HasValue && existing.TryGetValue(dto.Id.Value, out var found))
            {
                entity = found;
                keep.Add(found.ParticipationId);
            }
            else
            {
                entity = new ResearchProjectParticipation();
                project.Participations.Add(entity);
            }

            entity.IsExternal = dto.IsExternal;
            entity.PersonId = dto.IsExternal ? null : dto.PersonId;
            entity.ExternalFullName = dto.IsExternal ? dto.ExternalFullName!.Trim() : null;
            entity.ExternalIdentification = dto.IsExternal ? dto.ExternalIdentification!.Trim() : null;
            entity.ExternalInstitution = dto.IsExternal ? NullIfEmpty(dto.ExternalInstitution) : null;
            entity.RoleTypeId = dto.RoleTypeId;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
        }

        foreach (var (id, entity) in existing)
            if (!keep.Contains(id)) entity.IsDeleted = true;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<(StoredFile Stored, FileUploadResponseDto Physical)> UploadAsync(IFormFile file, string folder, CancellationToken ct)
    {
        var physical = await _fileManagement.UploadFileAsync(new FileUploadRequestDto
        {
            DirectoryCode = DirectoryCode,
            RelativePath = folder,
            FileName = file.FileName,
            File = file,
        }, ct);

        if (!physical.Success) throw new BusinessRuleException(physical.Message ?? "No se pudo subir el archivo.");

        // EntityId se completa con el Id del proyecto al guardar (el proyecto referencia al archivo por FK).
        return (StoredFileBuilder.Build(DirectoryCode, EntityTypeName, "0", file, physical, null), physical);
    }

    // ── Armado de DTOs ───────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<ResearchProjectDto>> BuildDtosAsync(IReadOnlyList<ResearchProject> projects, int? personFilter, CancellationToken ct)
    {
        if (projects.Count == 0) return [];

        var fileIds = projects.SelectMany(p => new int?[] { p.ApprovalFileId, p.ExtensionFileId, p.CompletionFileId }).Where(x => x.HasValue).Select(x => x!.Value);
        var files = await _repository.GetStoredFilesAsync(fileIds, ct);
        var refTypes = (await _repository.GetRefTypesAsync([RoleCategory], ct)).ToDictionary(r => r.Id);
        var personIds = projects.SelectMany(p => p.Participations).Where(x => !x.IsExternal && x.PersonId.HasValue).Select(x => x.PersonId!.Value);
        var people = await _repository.GetPeopleAsync(personIds, ct);

        ResearchResolutionDto Resolution(string number, int? fileId)
        {
            files.TryGetValue(fileId ?? 0, out var f);
            return new ResearchResolutionDto(number, f is null ? null : DocumentUrl(f.FileGuid), f?.OriginalFileName);
        }

        return projects.Select(p =>
        {
            ResearchExtensionDto? extension = null;
            if (p.HasExtension && p.ExtensionEndDate.HasValue)
            {
                files.TryGetValue(p.ExtensionFileId ?? 0, out var ef);
                extension = new ResearchExtensionDto(
                    p.ExtensionResolutionNumber ?? "",
                    p.ExtensionEndDate.Value,
                    ResearchDuration.Between(p.EndDate.AddDays(1), p.ExtensionEndDate.Value),
                    ef is null ? null : DocumentUrl(ef.FileGuid),
                    ef?.OriginalFileName);
            }

            var participations = p.Participations
                .Where(x => !x.IsDeleted && (personFilter is null || (!x.IsExternal && x.PersonId == personFilter)))
                .OrderBy(x => x.StartDate).ThenBy(x => x.ParticipationId)
                .Select(x =>
                {
                    refTypes.TryGetValue(x.RoleTypeId, out var role);
                    people.TryGetValue(x.PersonId ?? 0, out var person);
                    return new ResearchParticipationDto(
                        x.ParticipationId, $"PAR-{x.ParticipationId:D4}", x.IsExternal, x.PersonId,
                        x.IsExternal ? x.ExternalFullName ?? "" : person?.FullName ?? "",
                        x.IsExternal ? x.ExternalIdentification : person?.IdCard,
                        x.ExternalInstitution,
                        x.RoleTypeId, role?.Name ?? "", role?.Description ?? role?.Name ?? "",
                        x.StartDate, x.EndDate, ResearchDuration.Between(x.StartDate, x.EndDate));
                })
                .ToList();

            return new ResearchProjectDto(
                p.ResearchProjectId, ProjectCode(p.ResearchProjectId), p.Name, p.StartDate, p.EndDate,
                ResearchDuration.Between(p.StartDate, p.EndDate),
                Resolution(p.ApprovalResolutionNumber, p.ApprovalFileId),
                p.HasExtension, extension,
                Resolution(p.CompletionResolutionNumber, p.CompletionFileId),
                participations);
        }).ToList();
    }

    private static string ProjectCode(int id) => $"PRJ-{id:D4}";

    /// <summary>URL del documento, generada por el sistema; se descarga con la misma sesión/token del servicio.</summary>
    private string DocumentUrl(Guid fileGuid)
    {
        var path = $"/api/v1/rh/research-projects/documents/{fileGuid}";
        var request = _http.HttpContext?.Request;
        return request is null ? path : $"{request.Scheme}://{request.Host}{request.PathBase}{path}";
    }
}

/// <summary>Cálculo del tiempo entre dos fechas (ambas inclusive) en años, meses y días.</summary>
public static class ResearchDuration
{
    public static DurationDto Between(DateOnly start, DateOnly end)
    {
        if (end < start) return new DurationDto(0, 0, 0, 0, "0 días");

        var totalDays = end.DayNumber - start.DayNumber + 1;
        var limit = end.AddDays(1); // el día final cuenta
        var totalMonths = ((limit.Year - start.Year) * 12) + limit.Month - start.Month;
        if (limit.Day < start.Day) totalMonths--;
        if (totalMonths < 0) totalMonths = 0;

        var days = limit.DayNumber - start.AddMonths(totalMonths).DayNumber;
        var years = totalMonths / 12;
        var months = totalMonths % 12;

        var parts = new List<string>();
        if (years > 0) parts.Add(years == 1 ? "1 año" : $"{years} años");
        if (months > 0) parts.Add(months == 1 ? "1 mes" : $"{months} meses");
        if (days > 0) parts.Add(days == 1 ? "1 día" : $"{days} días");

        var text = parts.Count switch
        {
            0 => "0 días",
            1 => parts[0],
            _ => string.Join(", ", parts.Take(parts.Count - 1)) + " y " + parts[^1],
        };

        return new DurationDto(years, months, days, totalDays, text);
    }
}

using System.Globalization;
using WsUtaSystem.Application.Common.Extensions;
using WsUtaSystem.Application.DTOs.AcademicCv;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <inheritdoc cref="IAcademicCvService"/>
public class AcademicCvService : IAcademicCvService
{
    private readonly IPeopleService _people;
    private readonly IEmployeesService _employees;
    private readonly IEmployeeLaborRegimeService _laborRegime;
    private readonly ITeacherStructureService _teacherStructure;
    private readonly IEducationLevelsService _educationLevels;
    private readonly IWorkExperiencesService _workExperiences;
    private readonly IPublicationsService _publications;
    private readonly IBooksService _books;
    private readonly ITrainingsService _trainings;
    private readonly ILanguagesService _languages;
    private readonly IRefTypesService _refTypes;
    private readonly IInstitutionsService _institutions;
    private readonly IKnowledgeAreaService _knowledgeAreas;
    private readonly IDocumentOrchestratorService _documents;

    private const string PhotoDirectoryCode = "HR_HOJA_DE_VIDA";
    private const string PhotoEntityType = "PERSON";

    private static readonly CultureInfo EsEc = CultureInfo.GetCultureInfo("es-EC");

    public AcademicCvService(
        IPeopleService people,
        IEmployeesService employees,
        IEmployeeLaborRegimeService laborRegime,
        ITeacherStructureService teacherStructure,
        IEducationLevelsService educationLevels,
        IWorkExperiencesService workExperiences,
        IPublicationsService publications,
        IBooksService books,
        ITrainingsService trainings,
        ILanguagesService languages,
        IRefTypesService refTypes,
        IInstitutionsService institutions,
        IKnowledgeAreaService knowledgeAreas,
        IDocumentOrchestratorService documents)
    {
        _people = people;
        _employees = employees;
        _laborRegime = laborRegime;
        _teacherStructure = teacherStructure;
        _educationLevels = educationLevels;
        _workExperiences = workExperiences;
        _publications = publications;
        _books = books;
        _trainings = trainings;
        _languages = languages;
        _refTypes = refTypes;
        _institutions = institutions;
        _knowledgeAreas = knowledgeAreas;
        _documents = documents;
    }

    public async Task<AcademicCvDto?> BuildAsync(int personId, CancellationToken ct)
    {
        var person = await _people.GetByIdAsync(personId, ct);
        if (person is null) return null;

        // Secuencial a propósito: AppDbContext es Scoped (una instancia por request,
        // compartida por todos estos servicios) y EF Core no admite operaciones
        // concurrentes sobre la misma instancia — lanzar estas consultas en paralelo
        // (Task.WhenAll) revienta con "A second operation was started on this context
        // instance...". El costo real es despreciable: son consultas filtradas por
        // una sola persona (pocos registros cada una), disparadas por un clic humano de
        // exportación, no un endpoint de alto volumen.
        var employeeRecords = (await _employees.GetByPersonIdAsync(personId, ct)).ToList();
        // Si hay varios (poco común — ej. un vínculo cerrado histórico y uno nuevo), se
        // prioriza el activo más reciente; si ninguno está activo, el más reciente de todos.
        var employee = employeeRecords
            .Where(e => e.IsActive && !e.IsDeleted)
            .OrderByDescending(e => e.HireDate)
            .FirstOrDefault()
            ?? employeeRecords.OrderByDescending(e => e.HireDate).FirstOrDefault();

        var laborRegimes = employee is not null
            ? (await _laborRegime.GetByEmployeeAsync(employee.EmployeeId, ct)).Where(r => r.IsActive).ToList()
            : new List<Application.DTOs.EmployeeLaborRegime.EmployeeLaborRegimeDto>();

        var teacherStructures = employee is not null
            ? (await _teacherStructure.GetByEmployeeAsync(employee.EmployeeId, ct)).Where(t => t.IsActive).ToList()
            : new List<Application.DTOs.TeacherStructure.TeacherStructureDto>();

        var education = (await _educationLevels.GetByPersonIdAsync(personId))
            .OrderByDescending(e => e.StartDate).ToList();
        var experience = (await _workExperiences.GetByPersonIdAsync(personId))
            .OrderByDescending(e => e.StartDate).ToList();
        var publications = (await _publications.GetByPersonIdAsync(personId))
            .OrderByDescending(p => p.PublicationDate).ToList();
        var books = (await _books.GetByPersonIdAsync(personId))
            .OrderByDescending(b => b.PublicationDate).ToList();
        var trainings = (await _trainings.GetByPersonIdAsync(personId))
            .OrderByDescending(t => t.StartDate).ToList();
        var languages = (await _languages.GetByPersonIdAsync(personId))
            .OrderByDescending(l => l.IssueDate).ToList();

        // ref_Types e Institutions son catálogos pequeños — se cargan completos una sola
        // vez para resolver todos los *TypeId en memoria, sin ida y vuelta por registro.
        var refTypeNames = (await _refTypes.GetAllAsync(ct)).ToDictionary(r => r.TypeId, r => r.Name);
        var institutionNames = (await _institutions.GetAllAsync(ct)).ToDictionary(i => i.InstitutionId, i => i.Name);

        string? ResolveRefType(int? typeId) =>
            typeId.HasValue && refTypeNames.TryGetValue(typeId.Value, out var name) ? name : null;

        string? ResolveInstitution(int? institutionId) =>
            institutionId.HasValue && institutionNames.TryGetValue(institutionId.Value, out var name) ? name : null;

        string FormatMonthYear(DateOnly? date) =>
            date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue).ToString("MMM yyyy", EsEc) : string.Empty;

        string FormatDateRange(DateOnly? start, DateOnly? end, bool? isCurrent = null)
        {
            var startLabel = FormatMonthYear(start);
            if (isCurrent == true) return $"{startLabel} — Actual";
            var endLabel = end.HasValue ? FormatMonthYear(end) : "Actual";
            return string.IsNullOrEmpty(startLabel) ? endLabel : $"{startLabel} — {endLabel}";
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        int? CalculateAge(DateOnly? birthDate)
        {
            if (!birthDate.HasValue) return null;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value > today.AddYears(-age)) age--;
            return age;
        }

        int? CalculateYearsOfService(DateOnly? hireDate)
        {
            if (!hireDate.HasValue) return null;
            var years = today.Year - hireDate.Value.Year;
            if (hireDate.Value > today.AddYears(-years)) years--;
            return Math.Max(years, 0);
        }

        var dto = new AcademicCvDto
        {
            PersonId = personId,
            FullName = person.GetFullName(),
            IdCard = person.IdCard,
            Age = CalculateAge(person.BirthDate),
            // Institucional (@uta.edu.ec) vive en Employees.Email, no en People.Email —
            // null si la persona no tiene (o nunca tuvo) un registro de empleado.
            Email = employee?.Email,
            YearsOfService = employee is not null ? CalculateYearsOfService(employee.HireDate) : null,

            LaborRegimes = laborRegimes.Select(r => new AcademicCvLaborRegimeItem
            {
                RegimeName = r.LaborRegimeName ?? "Régimen no especificado",
                JobName = r.JobName,
                DepartmentName = r.DepartmentName,
                IsIndefinite = r.IsIndefinite,
                SinceLabel = FormatMonthYear(r.EffectiveFrom),
            }).ToList(),

            TeacherStructures = teacherStructures.Select(t => new AcademicCvTeacherStructureItem
            {
                LadderName = t.LadderName,
                DedicationName = t.DedicationName,
                WeeklyClassHours = t.WeeklyClassHours,
                DepartmentName = t.DepartmentName,
                SinceLabel = FormatMonthYear(t.StartDate),
            }).ToList(),

            EducationLevels = education.Select(e => new AcademicCvEducationItem
            {
                Title = e.Title,
                Specialty = e.Specialty,
                LevelName = ResolveRefType(e.EducationLevelTypeId),
                InstitutionName = ResolveInstitution(e.InstitutionId) ?? e.InstitutionNameOriginal,
                DateRangeLabel = FormatDateRange(e.StartDate, e.EndDate),
                SenescytRegistrationNumber = e.SenescytRegistrationNumber,
            }).ToList(),

            WorkExperiences = experience.Select(e => new AcademicCvWorkExperienceItem
            {
                Position = e.Position,
                Company = e.Company,
                ExperienceTypeName = ResolveRefType(e.ExperienceTypeId),
                DateRangeLabel = FormatDateRange(e.StartDate, e.EndDate, e.IsCurrent),
            }).ToList(),

            Publications = publications.Select(p => new AcademicCvPublicationItem
            {
                Title = p.Title,
                PublicationTypeName = ResolveRefType(p.PublicationTypeId),
                JournalName = p.JournalName,
                DateLabel = p.PublicationDate.HasValue ? FormatMonthYear(p.PublicationDate) : null,
            }).ToList(),

            Books = books.Select(b => new AcademicCvBookItem
            {
                Title = b.Title,
                Publisher = b.Publisher,
                DateLabel = b.PublicationDate.HasValue ? FormatMonthYear(b.PublicationDate) : null,
                PeerReviewed = b.PeerReviewed ?? false,
            }).ToList(),

            Trainings = trainings.Select(t => new AcademicCvTrainingItem
            {
                Title = t.Title,
                Institution = t.Institution,
                DateRangeLabel = FormatDateRange(t.StartDate, t.EndDate),
                Hours = t.Hours,
            }).ToList(),

            Languages = languages.Select(l => new AcademicCvLanguageItem
            {
                LanguageName = ResolveRefType(l.LanguageTypeId) ?? "Idioma",
                LevelName = ResolveRefType(l.LevelTypeId),
                CertifyingInstitution = l.CertifyingInstitution,
            }).ToList(),
        };

        // Áreas de conocimiento referenciadas por la formación académica (tbl_KnowledgeArea,
        // catálogo jerárquico distinto de ref_Types — ver comentario en EducationLevels.KnowledgeAreaId).
        var knowledgeAreaIds = education
            .Where(e => e.KnowledgeAreaId.HasValue)
            .Select(e => e.KnowledgeAreaId!.Value)
            .Distinct()
            .ToList();

        var knowledgeAreaNames = new List<string>();
        foreach (var areaId in knowledgeAreaIds)
        {
            var area = await _knowledgeAreas.GetByIdAsync(areaId, ct);
            if (area is not null) knowledgeAreaNames.Add(area.Name);
        }
        dto.KnowledgeAreas = knowledgeAreaNames.Distinct().OrderBy(n => n).ToList();

        // Fotografía de perfil — mismo directorio/entityType que ya usa PersonAvatarPreview
        // en el frontend (HR_HOJA_DE_VIDA / PERSON / personId). Toma la más reciente si por
        // alguna razón hay más de una activa; queda null si la persona no tiene foto.
        var photos = await _documents.ListByEntityAsync(
            PhotoDirectoryCode, PhotoEntityType, personId.ToString(), null, status: 1, ct);
        var latestPhoto = photos.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        if (latestPhoto is not null)
        {
            var downloaded = await _documents.DownloadByGuidAsync(latestPhoto.FileGuid, ct);
            dto.PhotoBytes = downloaded?.fileBytes;
        }

        return dto;
    }
}

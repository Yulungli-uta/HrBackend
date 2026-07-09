using Microsoft.Extensions.Options;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Options;
using WsUtaSystem.Application.DTOs.AcademicPromotion;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Arma el perfil académico docente completo. En modo mock (<see cref="AcademicPromotionOptions.UseMockData"/>)
/// delega en <see cref="IAcademicPromotionMockProvider"/>; en modo base de datos reutiliza los
/// repositorios existentes de hoja de vida (WorkExperiences, Publications, Books, Trainings, Languages)
/// y de estructura docente (TeacherStructure/AcademicLadder), sin duplicar su lógica.
/// </summary>
public sealed class AcademicPromotionService : IAcademicPromotionService
{
    private static readonly Dictionary<string, string> DedicationLabels = new()
    {
        ["TC"] = "FULL_TIME",
        ["EXCLUSIVA"] = "FULL_TIME_EXCLUSIVE",
        ["MT"] = "PART_TIME",
        ["HORAS"] = "HOURLY",
    };

    private const string PedagogicalCertificateName = "CURSO EN EL CAMPO DE DOCENCIA UNIVERSITARIA";
    private const string DisciplinaryCertificateName = "ACTUALIZACIÓN Y PERFECCIONAMIENTO EN EL CAMPO ESPECÍFICO";
    private static readonly string[] AllowedRoles = ["R_RH", "Administrador"];

    private readonly IOptions<AcademicPromotionOptions> _options;
    private readonly IAcademicPromotionMockProvider _mockProvider;
    private readonly IAcademicPromotionRepository _academicPromotionRepository;
    private readonly ITeacherStructureRepository _teacherStructureRepository;
    private readonly IWorkExperiencesRepository _workExperiencesRepository;
    private readonly IPublicationsRepository _publicationsRepository;
    private readonly IBooksRepository _booksRepository;
    private readonly ITrainingsRepository _trainingsRepository;
    private readonly ILanguagesRepository _languagesRepository;
    private readonly ICurrentUserService _currentUser;

    public AcademicPromotionService(
        IOptions<AcademicPromotionOptions> options,
        IAcademicPromotionMockProvider mockProvider,
        IAcademicPromotionRepository academicPromotionRepository,
        ITeacherStructureRepository teacherStructureRepository,
        IWorkExperiencesRepository workExperiencesRepository,
        IPublicationsRepository publicationsRepository,
        IBooksRepository booksRepository,
        ITrainingsRepository trainingsRepository,
        ILanguagesRepository languagesRepository,
        ICurrentUserService currentUser)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _mockProvider = mockProvider ?? throw new ArgumentNullException(nameof(mockProvider));
        _academicPromotionRepository = academicPromotionRepository ?? throw new ArgumentNullException(nameof(academicPromotionRepository));
        _teacherStructureRepository = teacherStructureRepository ?? throw new ArgumentNullException(nameof(teacherStructureRepository));
        _workExperiencesRepository = workExperiencesRepository ?? throw new ArgumentNullException(nameof(workExperiencesRepository));
        _publicationsRepository = publicationsRepository ?? throw new ArgumentNullException(nameof(publicationsRepository));
        _booksRepository = booksRepository ?? throw new ArgumentNullException(nameof(booksRepository));
        _trainingsRepository = trainingsRepository ?? throw new ArgumentNullException(nameof(trainingsRepository));
        _languagesRepository = languagesRepository ?? throw new ArgumentNullException(nameof(languagesRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public Task<bool> IsCurrentUserAuthorizedAsync(CancellationToken ct = default)
        => _academicPromotionRepository.UserHasAnyRoleAsync(_currentUser.UserId?.ToString(), AllowedRoles, ct);

    public async Task<TeacherAcademicProfileDto?> GetProfileByIdentificationAsync(string identification, CancellationToken ct = default)
    {
        if (_options.Value.UseMockData)
            return _mockProvider.GetProfile(identification);

        var employee = await _academicPromotionRepository.FindEmployeeByIdentificationAsync(identification, ct);
        if (employee is null) return null;

        var evaluationDate = DateOnly.FromDateTime(DateTime.Today);

        var teacherStructures = await _teacherStructureRepository.GetByEmployeeAsync(employee.EmployeeId, ct);
        var currentStructure = teacherStructures.FirstOrDefault(t => t.IsActive);

        var dependency = await _academicPromotionRepository.FindFacultyDependencyAsync(
            currentStructure?.DepartmentId ?? employee.DepartmentId, ct);

        var workExperiences = (await _workExperiencesRepository.GetByPersonIdAsync(employee.PersonId)).ToList();
        var publications = (await _publicationsRepository.GetByPersonIdAsync(employee.PersonId)).ToList();
        var books = (await _booksRepository.GetByPersonIdAsync(employee.PersonId)).ToList();
        var trainings = (await _trainingsRepository.GetByPersonIdAsync(employee.PersonId)).ToList();
        var languages = (await _languagesRepository.GetByPersonIdAsync(employee.PersonId)).ToList();

        // Resolución en lote de todos los nombres de ref_Types usados (evita N+1).
        var typeIds = workExperiences.Select(w => w.ExperienceTypeId)
            .Concat(publications.Select(p => p.KnowledgeAreaTypeId))
            .Concat(books.Select(b => b.KnowledgeAreaTypeId))
            .Concat(trainings.Select(t => (int?)t.KnowledgeAreaTypeId))
            .Concat(trainings.Select(t => t.CertificateTypeId))
            .Concat(trainings.Select(t => t.ModalityTypeId))
            .Concat(trainings.Select(t => t.TrainingDirectionTypeId))
            .Concat(languages.Select(l => (int?)l.LanguageTypeId))
            .Concat(languages.Select(l => (int?)l.LevelTypeId));
        var refTypeNames = await _academicPromotionRepository.GetRefTypeNamesAsync(typeIds, ct);

        string? ResolveName(int? typeId) => typeId.HasValue && refTypeNames.TryGetValue(typeId.Value, out var name) ? name : null;

        return new TeacherAcademicProfileDto(
            TeacherId: $"DOC-{employee.EmployeeId:D6}",
            IdentificationType: "CEDULA",
            Identification: employee.IdCard,
            FullName: employee.FullName,
            Orcid: null, // no existe columna en el modelo actual
            Dependency: dependency is not null
                ? new DependencyDto($"DEP-{dependency.DepartmentId:D3}", dependency.Name)
                : new DependencyDto("", ""),
            EmploymentRelationship: currentStructure is not null
                ? MapDedicationLabel(ResolveName(currentStructure.DedicationTypeId))
                : null,
            CurrentPosition: currentStructure?.Ladder is not null ? MapLadderCode(currentStructure.Ladder.Code) : null,
            CurrentPositionStartDate: currentStructure?.StartDate,
            EvaluationDate: evaluationDate,
            Experience: workExperiences.Select(w => MapExperience(w, ResolveName(w.ExperienceTypeId), evaluationDate)).ToList(),
            // Los libros se reportan como parte de "publications" (Type="BOOK") -- son un tipo
            // más de producción académica, no un módulo aparte en el JSON de perfil.
            Publications: publications.Select(p => MapPublication(p, ResolveName(p.KnowledgeAreaTypeId)))
                .Concat(books.Select(b => MapBook(b, ResolveName(b.KnowledgeAreaTypeId))))
                .ToList(),
            ReceivedTrainings: trainings
                .Where(t => IsDirection(t.TrainingDirectionTypeId, TrainingDirection.Received, refTypeNames))
                .Select(t => MapTraining(t, TrainingDirection.Received, ResolveName(t.KnowledgeAreaTypeId), ResolveName(t.CertificateTypeId), ResolveName(t.ModalityTypeId)))
                .ToList(),
            GivenTrainings: trainings
                .Where(t => IsDirection(t.TrainingDirectionTypeId, TrainingDirection.Given, refTypeNames))
                .Select(t => MapTraining(t, TrainingDirection.Given, ResolveName(t.KnowledgeAreaTypeId), ResolveName(t.CertificateTypeId), ResolveName(t.ModalityTypeId)))
                .ToList(),
            ResearchProjects: [], // modulo no implementado todavia
            DoctoralTheses: [],   // modulo no implementado todavia
            Languages: languages.Select(l => MapLanguage(l, ResolveName(l.LanguageTypeId), ResolveName(l.LevelTypeId))).ToList(),
            Score: null // modulo de evaluacion de desempeno no implementado todavia
        );
    }

    private static bool IsDirection(int? directionTypeId, string expected, IReadOnlyDictionary<int, string> refTypeNames)
        => directionTypeId.HasValue && refTypeNames.TryGetValue(directionTypeId.Value, out var name) && name == expected;

    private static string MapDedicationLabel(string? dedicationName)
        => dedicationName is not null && DedicationLabels.TryGetValue(dedicationName, out var label) ? label : dedicationName ?? "";

    private static string MapLadderCode(string code)
        => code.StartsWith("TITULAR_", StringComparison.OrdinalIgnoreCase) ? code["TITULAR_".Length..] : code;

    /// <summary>
    /// Años/meses de UNA experiencia individual (no del total acumulado). El total agregado
    /// para validar requisitos de promoción debe fusionar rangos solapados antes de sumar
    /// (merge de intervalos) — pendiente para la futura capa de validación, no aplica aquí
    /// porque cada item se reporta de forma independiente.
    /// </summary>
    private static TeacherExperienceDto MapExperience(WorkExperiences w, string? knowledgeArea, DateOnly evaluationDate)
    {
        var end = w.EndDate ?? evaluationDate;
        var totalMonths = ((end.Year - w.StartDate.Year) * 12) + (end.Month - w.StartDate.Month);
        if (end.Day < w.StartDate.Day) totalMonths--;
        if (totalMonths < 0) totalMonths = 0;

        return new TeacherExperienceDto(
            Id: $"EXP-{w.WorkExpId:000}",
            Type: "TEACHING_EXPERIENCE",
            Institution: w.Company,
            Position: w.Position,
            Category: null, // no existe columna en el modelo actual
            StartDate: w.StartDate,
            EndDate: w.EndDate,
            Years: totalMonths / 12,
            Months: totalMonths,
            KnowledgeArea: knowledgeArea,
            Country: w.CountryId,
            SupportingDocumentUrl: null // WorkExperiences no tiene vinculo a StoredFile hoy
        );
    }

    private static TeacherPublicationDto MapPublication(Publications p, string? knowledgeArea) => new(
        Id: $"PUB-{p.PublicationId:000}",
        Type: "SCIENTIFIC_ARTICLE",
        Name: p.Title,
        Journal: p.JournalName,
        KnowledgeArea: knowledgeArea,
        PublicationDate: p.PublicationDate ?? default,
        Doi: null,        // no existe columna en el modelo actual
        Link: null,       // no existe columna en el modelo actual
        Language: null,   // no existe columna en el modelo actual
        IndexingDatabase: p.IsIndexed == true ? "INDEXED" : null,
        Status: "PUBLISHED",
        Country: null,    // no existe columna en el modelo actual
        SupportingDocumentUrl: null // Publications no tiene vinculo a StoredFile hoy
    );

    private static TeacherPublicationDto MapBook(Books b, string? knowledgeArea) => new(
        Id: $"BOOK-{b.BookId:000}",
        Type: "BOOK",
        Name: b.Title,
        Journal: b.Publisher,
        KnowledgeArea: knowledgeArea,
        PublicationDate: b.PublicationDate ?? default,
        Doi: null,
        Link: null,
        Language: null,
        IndexingDatabase: null,
        Status: "PUBLISHED",
        Country: b.CountryId,
        SupportingDocumentUrl: null // Books no tiene vinculo a StoredFile hoy
    );

    private static TeacherTrainingDto MapTraining(Trainings t, string direction, string? knowledgeArea, string? certificateTypeName, string? modality) => new(
        Id: $"TRN-{t.TrainingId:000}",
        Type: direction,
        TrainingCategory: certificateTypeName switch
        {
            PedagogicalCertificateName => "PEDAGOGICAL",
            DisciplinaryCertificateName => "DISCIPLINARY",
            _ => null,
        },
        Name: t.Title,
        Institution: t.Institution,
        StartDate: t.StartDate,
        EndDate: t.EndDate,
        Hours: t.Hours,
        KnowledgeArea: knowledgeArea,
        Modality: modality,
        Country: t.CountryId,
        SupportingDocumentUrl: null // Trainings no tiene vinculo a StoredFile hoy
    );

    private static TeacherLanguageDto MapLanguage(Languages l, string? language, string? level) => new(
        Id: $"LAN-{l.LanguageId:000}",
        Type: "LANGUAGE_CERTIFICATION",
        Language: language ?? "",
        Level: level ?? "",
        ReferenceFramework: l.ReferenceFramework,
        CertifyingInstitution: l.CertifyingInstitution,
        Country: l.CountryId,
        IssueDate: l.IssueDate,
        ExpirationDate: l.ExpirationDate,
        SupportingDocumentUrl: null // se resuelve via DocumentsController (DirectoryCode=HR_LANGUAGE_CERTIFICATION) desde el frontend
    );
}

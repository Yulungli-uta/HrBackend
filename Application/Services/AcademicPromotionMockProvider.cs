using WsUtaSystem.Application.DTOs.AcademicPromotion;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <summary>
/// Genera perfiles académicos docentes sintéticos, completos y realistas.
/// Incluye un perfil distinto (docente ficticio distinto) por cada regla
/// referencial de promoción, seleccionable enviando el código correspondiente
/// como "identification" (MOCK001..MOCK005). Cualquier otra identificación
/// (incluida MOCK006) devuelve el escenario más exigente, Principal 2 → Principal 3,
/// que ya cumple de sobra los requisitos de las demás reglas.
///
/// MOCKFULL01..MOCKFULL03: perfiles "sin ningún campo nulo/vacío" -- a diferencia
/// de los MOCK001..006 (que dejan en null/[] los campos que esa regla de promoción
/// no exige, ej. GivenTrainings/DoctoralTheses en Auxiliar), estos garantizan que
/// TODOS los bloques y TODOS los campos opcionales dentro de cada item vengan
/// llenos (Experience.Category, Experience.EndDate, Languages.ExpirationDate,
/// etc.), útiles para probar que el consumidor del JSON maneja bien el caso
/// "toda la información existe", sin depender de que la BD real la tenga.
/// </summary>
public sealed class AcademicPromotionMockProvider : IAcademicPromotionMockProvider
{
    public TeacherAcademicProfileDto GetProfile(string identification)
    {
        return identification.Trim().ToUpperInvariant() switch
        {
            "MOCKFULL01" => BuildFullyPopulatedProfile(identification, "DOC-200001", "Roberto Iván Salazar Guerrero", "PRINCIPAL_3"),
            "MOCKFULL02" => BuildFullyPopulatedProfile(identification, "DOC-200002", "Gabriela Estefanía Ortiz Ramos", "PRINCIPAL_2"),
            "MOCKFULL03" => BuildFullyPopulatedProfile(identification, "DOC-200003", "Fernando Xavier Herrera Castillo", "AGREGADO_3"),
            // Auxiliar 1 -> Auxiliar 2: exp 4a, 1 publicación, 96h (25% pedagógica), idioma B1.
            "MOCK001" => BuildProfile(identification, "DOC-100001", "María Fernanda Torres Vaca", "AUXILIAR_1",
                experienceYears: 4, publicationsCount: 1, foreignPublications: 0,
                receivedHours: 96, pedagogicalHours: 24, givenHours: 0, researchWeightedMonths: 0, thesisCount: 0,
                languageLevel: "B1", score: 78.0m),

            // Auxiliar 2 -> Agregado 1: exp 4a, 2 publicaciones, 96h (25% pedagógica), idioma B1.
            "MOCK002" => BuildProfile(identification, "DOC-100002", "Carlos Andrés Molina Ruiz", "AUXILIAR_2",
                experienceYears: 4, publicationsCount: 2, foreignPublications: 0,
                receivedHours: 96, pedagogicalHours: 24, givenHours: 0, researchWeightedMonths: 0, thesisCount: 0,
                languageLevel: "B1", score: 80.0m),

            // Agregado 1 -> Agregado 2: exp 4a, 3 publicaciones, 128h (25% pedagógica), 24m investigación, idioma B1.
            "MOCK003" => BuildProfile(identification, "DOC-100003", "Diana Patricia Vargas Salas", "AGREGADO_1",
                experienceYears: 4, publicationsCount: 3, foreignPublications: 0,
                receivedHours: 128, pedagogicalHours: 32, givenHours: 0, researchWeightedMonths: 24, thesisCount: 0,
                languageLevel: "B1", score: 82.0m),

            // Agregado 2 -> Agregado 3: exp 4a, 5 publicaciones, 160h (25% pedagógica), 24m investigación, idioma B1.
            "MOCK004" => BuildProfile(identification, "DOC-100004", "Jorge Luis Chávez Peña", "AGREGADO_2",
                experienceYears: 4, publicationsCount: 5, foreignPublications: 0,
                receivedHours: 160, pedagogicalHours: 40, givenHours: 0, researchWeightedMonths: 24, thesisCount: 0,
                languageLevel: "B1", score: 83.0m),

            // Principal 1 -> Principal 2: exp 3a, 8 publicaciones (2 en idioma extranjero), 160h (25% pedagógica),
            // 40h impartida, 24m investigación, 2 tesis, idioma B1.
            "MOCK005" => BuildProfile(identification, "DOC-100005", "Verónica Alexandra Suárez Mora", "PRINCIPAL_1",
                experienceYears: 3, publicationsCount: 8, foreignPublications: 2,
                receivedHours: 160, pedagogicalHours: 40, givenHours: 40, researchWeightedMonths: 24, thesisCount: 2,
                languageLevel: "B1", score: 85.0m),

            // Principal 2 -> Principal 3 (escenario más exigente, también el default):
            // exp 3a, 12 publicaciones (4 en idioma extranjero), 256h (50% pedagógica),
            // 80h impartida, 54m investigación ponderados, 3 tesis, idioma B1/B2.
            _ => BuildProfile(identification, "DOC-000001", "Juan Carlos Pérez López", "PRINCIPAL_2",
                experienceYears: 3, publicationsCount: 12, foreignPublications: 4,
                receivedHours: 256, pedagogicalHours: 128, givenHours: 80, researchWeightedMonths: 54, thesisCount: 3,
                languageLevel: "B1", score: 85.0m),
        };
    }

    private static TeacherAcademicProfileDto BuildProfile(
        string identification, string teacherId, string fullName, string currentPosition,
        int experienceYears, int publicationsCount, int foreignPublications,
        int receivedHours, int pedagogicalHours, int givenHours, int researchWeightedMonths, int thesisCount,
        string languageLevel, decimal score)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return new TeacherAcademicProfileDto(
            TeacherId: teacherId,
            IdentificationType: "CEDULA",
            Identification: identification,
            FullName: fullName,
            Orcid: "0000-0002-1234-5678",
            Dependency: new DependencyDto("DEP-001", "Facultad de Ingeniería en Sistemas, Electrónica e Industrial"),
            EmploymentRelationship: "FULL_TIME",
            CurrentPosition: currentPosition,
            CurrentPositionStartDate: today.AddYears(-experienceYears),
            EvaluationDate: today,
            Experience: Experience(experienceYears),
            Publications: Publications(publicationsCount, foreignPublications),
            ReceivedTrainings: ReceivedTrainings(receivedHours, pedagogicalHours),
            GivenTrainings: GivenTrainings(givenHours),
            ResearchProjects: ResearchProjects(researchWeightedMonths),
            DoctoralTheses: DoctoralTheses(thesisCount),
            Languages: Languages(languageLevel),
            Score: new TeacherScoreDto("PERFORMANCE_EVALUATION", "2024", score, "https://example.com/performance-evaluation.pdf")
        );
    }

    /// <summary>
    /// Perfil sin ningún campo nulo/vacío: 2 experiencias (ambas con Category y EndDate
    /// llenos), publicaciones, ambas direcciones de capacitación, investigación, tesis,
    /// 2 idiomas (uno de ellos con ExpirationDate llena) y evaluación de desempeño.
    /// </summary>
    private static TeacherAcademicProfileDto BuildFullyPopulatedProfile(
        string identification, string teacherId, string fullName, string currentPosition)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return new TeacherAcademicProfileDto(
            TeacherId: teacherId,
            IdentificationType: "CEDULA",
            Identification: identification,
            FullName: fullName,
            Orcid: "0000-0002-9876-5432",
            Dependency: new DependencyDto("DEP-001", "Facultad de Ingeniería en Sistemas, Electrónica e Industrial"),
            EmploymentRelationship: "FULL_TIME",
            CurrentPosition: currentPosition,
            CurrentPositionStartDate: today.AddYears(-5),
            EvaluationDate: today,
            Experience:
            [
                new("EXP-FULL-001", "TEACHING_EXPERIENCE", "Universidad Técnica de Ambato", "Profesor Titular", "PRINCIPAL",
                    today.AddYears(-5), today.AddYears(-2), 3, 36,
                    "Information and Communication Technologies", "EC", "https://example.com/experience-full-001.pdf"),
                new("EXP-FULL-002", "TEACHING_EXPERIENCE", "Escuela Politécnica Nacional", "Profesor Ocasional", "AGREGADO",
                    today.AddYears(-8), today.AddYears(-5), 3, 36,
                    "Software Engineering", "EC", "https://example.com/experience-full-002.pdf"),
            ],
            Publications: Publications(6, 2),
            ReceivedTrainings: ReceivedTrainings(160, 48),
            GivenTrainings: GivenTrainings(48),
            ResearchProjects: ResearchProjects(36),
            DoctoralTheses: DoctoralTheses(2),
            Languages:
            [
                new("LAN-FULL-001", "LANGUAGE_CERTIFICATION", "EN", "B2", "CEFR", "Cambridge Assessment English", "GB",
                    today.AddYears(-2), today.AddYears(3), "https://example.com/language-full-001.pdf"),
                new("LAN-FULL-002", "LANGUAGE_CERTIFICATION", "FR", "B1", "CEFR", "Alliance Française", "FR",
                    today.AddYears(-3), today.AddYears(2), "https://example.com/language-full-002.pdf"),
            ],
            Score: new TeacherScoreDto("PERFORMANCE_EVALUATION", "2024", 88.5m, "https://example.com/performance-evaluation-full.pdf")
        );
    }

    private static IReadOnlyList<TeacherExperienceDto> Experience(int years)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return
        [
            new($"EXP-{years:00}Y", "TEACHING_EXPERIENCE", "Universidad Técnica de Ambato", "Profesor Titular", null,
                today.AddYears(-years), null, years, years * 12,
                "Information and Communication Technologies", "EC", "https://example.com/experience-001.pdf"),
        ];
    }

    private static IReadOnlyList<TeacherPublicationDto> Publications(int count, int foreignCount)
    {
        var list = new List<TeacherPublicationDto>(count);
        for (var i = 1; i <= count; i++)
        {
            var isForeign = i <= foreignCount;
            list.Add(new TeacherPublicationDto(
                Id: $"PUB-{i:000}",
                Type: "SCIENTIFIC_ARTICLE",
                Name: isForeign ? $"Research Topic in Higher Education {i}" : $"Tema de Investigación Educativa {i}",
                Journal: $"Revista / Journal Académico {i}",
                KnowledgeArea: "Information and Communication Technologies",
                PublicationDate: new DateOnly(2021 + (i % 4), (i % 12) + 1, 10),
                Doi: $"10.0000/mock-{i:000}",
                Link: $"https://example.com/publication-{i:000}",
                Language: isForeign ? "EN" : "ES",
                IndexingDatabase: i % 2 == 0 ? "Scopus" : "Latindex",
                Status: "PUBLISHED",
                Country: isForeign ? "US" : "EC",
                SupportingDocumentUrl: $"https://example.com/publication-{i:000}.pdf"
            ));
        }
        return list;
    }

    private static IReadOnlyList<TeacherTrainingDto> ReceivedTrainings(int totalHours, int pedagogicalHours)
    {
        if (totalHours <= 0) return [];
        var disciplinaryHours = totalHours - pedagogicalHours;

        var list = new List<TeacherTrainingDto>
        {
            new("RTRN-001", TrainingDirection.Received, "PEDAGOGICAL", "Metodologías de Docencia Universitaria",
                "Universidad Técnica de Ambato", new DateOnly(2024, 1, 10), new DateOnly(2024, 2, 10), pedagogicalHours,
                "Education", "ONLINE", "EC", "https://example.com/received-training-001.pdf"),
        };

        if (disciplinaryHours > 0)
        {
            list.Add(new TeacherTrainingDto("RTRN-002", TrainingDirection.Received, "DISCIPLINARY",
                "Actualización en el Campo Disciplinar", "Universidad Técnica de Ambato",
                new DateOnly(2024, 3, 1), new DateOnly(2024, 4, 1), disciplinaryHours,
                "Software Engineering", "IN_PERSON", "EC", "https://example.com/received-training-002.pdf"));
        }

        return list;
    }

    private static IReadOnlyList<TeacherTrainingDto> GivenTrainings(int totalHours)
    {
        if (totalHours <= 0) return [];
        return
        [
            new("GTRN-001", TrainingDirection.Given, "DISCIPLINARY", "Capacitación Impartida a Pares Docentes",
                "Universidad Técnica de Ambato", new DateOnly(2024, 10, 1), new DateOnly(2024, 10, 15), totalHours,
                "Software Engineering", "IN_PERSON", "EC", "https://example.com/given-training-001.pdf"),
        ];
    }

    private static IReadOnlyList<TeacherResearchProjectDto> ResearchProjects(int weightedMonthsTarget)
    {
        if (weightedMonthsTarget <= 0) return [];

        // 1 proyecto como COORDINATOR (factor x2 según las reglas de promoción) alcanza
        // el objetivo ponderado con la mitad de meses reales.
        var realMonths = Math.Max(1, weightedMonthsTarget / 2);
        return
        [
            new("RES-001", "RESEARCH_PROJECT", "Proyecto de Investigación Institucional", "UTA-RES-MOCK-001",
                "Universidad Técnica de Ambato", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 1).AddMonths(realMonths),
                realMonths, "COORDINATOR", "Information and Communication Technologies", "COMPLETED", "EC",
                "https://example.com/research-001.pdf"),
        ];
    }

    private static IReadOnlyList<TeacherDoctoralThesisDto> DoctoralTheses(int count)
    {
        var list = new List<TeacherDoctoralThesisDto>(count);
        for (var i = 1; i <= count; i++)
        {
            list.Add(new TeacherDoctoralThesisDto(
                $"PHD-THESIS-{i:000}", "DOCTORAL_THESIS", $"Tesis Doctoral de Ejemplo {i}",
                "Universidad Example", new DateOnly(2022, i, 15), i % 2 == 0 ? "CO_DIRECTOR" : "DIRECTOR",
                "Information and Communication Technologies", "EC", $"https://example.com/doctoral-thesis-{i:000}.pdf"));
        }
        return list;
    }

    private static IReadOnlyList<TeacherLanguageDto> Languages(string level) =>
    [
        new("LAN-001", "LANGUAGE_CERTIFICATION", "EN", level, "CEFR", "Cambridge Assessment English", "GB",
            new DateOnly(2024, 1, 1), null, "https://example.com/language-001.pdf"),
    ];
}

namespace WsUtaSystem.Application.DTOs.AcademicCv;

/// <summary>
/// Datos ya resueltos (catálogos incluidos) para exportar la Hoja de Vida Académica en PDF.
/// Deliberadamente excluye datos personales sensibles (dirección, salud, contactos, cuentas
/// bancarias, cargas familiares) — solo trayectoria académica/profesional.
/// </summary>
public class AcademicCvDto
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = null!;
    public string IdCard { get; set; } = null!;
    public int? Age { get; set; }
    /// <summary>Bytes de la fotografía de perfil (HR_HOJA_DE_VIDA / entityType=PERSON), o
    /// null si la persona no tiene una foto activa registrada.</summary>
    public byte[]? PhotoBytes { get; set; }
    /// <summary>Correo institucional (Employees.Email, @uta.edu.ec) — dato de contacto
    /// profesional, no personal. Null si la persona no tiene registro de empleado.</summary>
    public string? Email { get; set; }
    /// <summary>Años de servicio calculados desde Employees.HireDate. Null si la persona no
    /// tiene un registro de empleado (ej. solo docente ocasional sin vínculo activo).</summary>
    public int? YearsOfService { get; set; }

    public List<AcademicCvLaborRegimeItem> LaborRegimes { get; set; } = new();
    public List<AcademicCvTeacherStructureItem> TeacherStructures { get; set; } = new();
    public List<AcademicCvEducationItem> EducationLevels { get; set; } = new();
    public List<AcademicCvWorkExperienceItem> WorkExperiences { get; set; } = new();
    public List<AcademicCvPublicationItem> Publications { get; set; } = new();
    public List<AcademicCvBookItem> Books { get; set; } = new();
    public List<AcademicCvTrainingItem> Trainings { get; set; } = new();
    public List<AcademicCvLanguageItem> Languages { get; set; } = new();

    /// <summary>Nombres de áreas de conocimiento (tbl_KnowledgeArea) referenciadas por la
    /// formación académica de la persona, ya deduplicadas.</summary>
    public List<string> KnowledgeAreas { get; set; } = new();
}

/// <summary>Régimen laboral activo (puede haber más de uno simultáneo, ej. nombramiento
/// LOSEP + contrato LOES). Solo se incluyen los activos — no es un historial completo.</summary>
public class AcademicCvLaborRegimeItem
{
    public string RegimeName { get; set; } = null!;
    public string? JobName { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsIndefinite { get; set; }
    public string SinceLabel { get; set; } = null!;
}

/// <summary>Estructura docente activa — lista vacía si la persona no es docente.</summary>
public class AcademicCvTeacherStructureItem
{
    public string? LadderName { get; set; }
    public string DedicationName { get; set; } = null!;
    public decimal? WeeklyClassHours { get; set; }
    public string? DepartmentName { get; set; }
    public string SinceLabel { get; set; } = null!;
}

public class AcademicCvEducationItem
{
    public string Title { get; set; } = null!;
    public string? Specialty { get; set; }
    public string? LevelName { get; set; }
    public string? InstitutionName { get; set; }
    public string? DateRangeLabel { get; set; }
    public string? SenescytRegistrationNumber { get; set; }
}

public class AcademicCvWorkExperienceItem
{
    public string Position { get; set; } = null!;
    public string Company { get; set; } = null!;
    public string? ExperienceTypeName { get; set; }
    public string DateRangeLabel { get; set; } = null!;
}

public class AcademicCvPublicationItem
{
    public string Title { get; set; } = null!;
    public string? PublicationTypeName { get; set; }
    public string? JournalName { get; set; }
    public string? DateLabel { get; set; }
}

public class AcademicCvBookItem
{
    public string Title { get; set; } = null!;
    public string? Publisher { get; set; }
    public string? DateLabel { get; set; }
    public bool PeerReviewed { get; set; }
}

public class AcademicCvTrainingItem
{
    public string Title { get; set; } = null!;
    public string Institution { get; set; } = null!;
    public string? DateRangeLabel { get; set; }
    public int Hours { get; set; }
}

public class AcademicCvLanguageItem
{
    public string LanguageName { get; set; } = null!;
    public string? LevelName { get; set; }
    public string? CertifyingInstitution { get; set; }
}

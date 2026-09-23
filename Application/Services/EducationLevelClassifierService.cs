using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Application.Services;

/// <summary>Resultado de clasificar un título de DINARDAP contra los catálogos ACADEMIC_LEVEL/SIIES_GRADO.</summary>
public sealed class EducationLevelClassificationResult
{
    public int? EducationLevelTypeId { get; init; }
    public int? SiiesGradoTypeId { get; init; }
    public bool RequiresManualReview { get; init; }
    public string? ReviewReason { get; init; }
}

public interface IEducationLevelClassifierService
{
    Task<EducationLevelClassificationResult> ClassifyAsync(
        string? nivelNombreDinardap, string? nombreTitulo, CancellationToken ct = default);
}

/// <summary>
/// Clasificador de 2 pasos, diseño aprobado por el usuario 2026-09-16:
/// 1) El nivel (Segundo/Tercer/Cuarto) sale SIEMPRE del `nivelNombre` que manda DINARDAP,
///    nunca se adivina del texto del título (un título "Doctora en X" puede ser Tercer o
///    Cuarto Nivel según DINARDAP mismo — confirmado con casos reales el 2026-09-15).
/// 2) Solo dentro de Cuarto Nivel se clasifica el grado (Doctorado/Maestría/Especialista/
///    Diplomado) por palabra clave en el título. Nunca se aplica a Tercer Nivel
///    (SiiesGradoTypeId queda null, tal como documenta el modelo original).
/// Ninguna variante no reconocida recibe un valor por defecto silencioso — siempre
/// RequiresManualReview = true en su lugar.
/// </summary>
public sealed class EducationLevelClassifierService : IEducationLevelClassifierService
{
    private const string AcademicLevelCategory = "ACADEMIC_LEVEL";
    private const string SiiesGradoCategory = "SIIES_GRADO";
    private const string CacheKey = "EducationLevelClassifier_RefTypes";

    // Nombres de ref_Types.Name en SIIES_GRADO (ver Database/hr/16_siies_profesores.sql).
    private const string GradoDoctorPhD = "Doctor (Ph.D)";
    private const string GradoMaestria = "Maestría o Equivalente";
    private const string GradoEspecialista = "Especialista";
    private const string GradoEspecialistaSalud = "Especialista Área Salud";
    private const string GradoDiplomaSuperior = "Diploma Superior";

    private readonly IRefTypesService _refTypes;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EducationLevelClassifierService> _logger;

    public EducationLevelClassifierService(
        IRefTypesService refTypes, IMemoryCache cache, ILogger<EducationLevelClassifierService> logger)
    {
        _refTypes = refTypes ?? throw new ArgumentNullException(nameof(refTypes));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EducationLevelClassificationResult> ClassifyAsync(
        string? nivelNombreDinardap, string? nombreTitulo, CancellationToken ct = default)
    {
        var (nivel1Fallback, nivel3, nivel4, grados) = await GetCatalogAsync(ct);
        var nivelTexto = (nivelNombreDinardap ?? string.Empty).Trim();

        int? nivelId = nivelTexto switch
        {
            "Tercer Nivel o Pregrado" => nivel3,
            "Educación Superior de Grado o Tercer Nivel" => nivel3,
            "Tercer Nivel Técnico Superior" => nivel3,
            // [2026-09-19] 3 variantes reales de DINARDAP encontradas en producción (430
            // registros atascados en NIVEL_1 antes de este fix, ver Database/hr sección 12):
            "TERCER_NIVEL" => nivel3,
            "Tercer Nivel Tecnológico Superior" => nivel3,
            "Tercer Nivel Tecnológico Superior Universitario" => nivel3,
            "Cuarto Nivel o Posgrado" => nivel4,
            "CUARTO_NIVEL" => nivel4,
            "Educación Superior de Posgrado o Cuarto Nivel" => nivel4,
            _ => null,
        };

        if (nivelId is null)
        {
            // EducationLevelTypeID es NOT NULL en la BD - no se puede dejar sin clasificar del
            // todo. Se usa NIVEL_1 (huérfano desde la corrección de 2026-09-16, ver
            // Database/hr/16_siies_profesores.sql sección 8.1) como bandeja de "pendiente de
            // revisión": el registro SÍ se crea (no se pierde el título), pero queda
            // reconocible por EducationLevelTypeId=NIVEL_1 + Source='Dinardap' para que alguien
            // lo reclasifique a mano, y SenescytNivelNombreOriginal guarda el texto real.
            _logger.LogWarning(
                "Nivel de DINARDAP no catalogado: '{Nivel}' (título: '{Titulo}') — va a revisión manual.",
                nivelTexto, nombreTitulo);
            return new EducationLevelClassificationResult
            {
                EducationLevelTypeId = nivel1Fallback,
                RequiresManualReview = true,
                ReviewReason = $"Nivel de DINARDAP no catalogado: \"{nivelTexto}\"",
            };
        }

        // El grado (Doctorado/Maestría/Especialista/Diplomado) solo aplica a Cuarto Nivel.
        if (nivelId != nivel4)
            return new EducationLevelClassificationResult { EducationLevelTypeId = nivelId };

        var titulo = (nombreTitulo ?? string.Empty).ToUpperInvariant();
        // Doble verificación de Cuarto Nivel (nivel calculado + texto crudo) antes de asumir
        // que "DOCTOR"/"DOCTORA" es un grado de doctorado - regla explícita del usuario.
        var cuartoNivelConfirmado = nivelTexto.Contains("CUARTO", StringComparison.OrdinalIgnoreCase);

        string? gradoNombre = null;
        // [2026-09-19] "DOUTOR" (portugués) agregado - 2 títulos reales en producción sin
        // clasificar por esto (programas brasileños vía DINARDAP).
        if (cuartoNivelConfirmado && (titulo.Contains("DOCTOR") || titulo.Contains("DOUTOR")))
            gradoNombre = GradoDoctorPhD;
        // [2026-09-19] "MAESTRA" (femenino, no es substring de MAESTRIA), "MÉSTER" (variante
        // con tilde) y "MESTRE" (portugués) agregados - mismo motivo que DOUTOR arriba.
        else if (titulo.Contains("MAGISTER") || titulo.Contains("MASTER") || titulo.Contains("MAESTRIA") ||
                 titulo.Contains("MAESTRÍA") || titulo.Contains("MAESTRA") || titulo.Contains("MÉSTER") ||
                 titulo.Contains("MESTRE"))
            gradoNombre = GradoMaestria;
        else if (titulo.Contains("ESPECIALISTA"))
            gradoNombre = (titulo.Contains("SALUD") || titulo.Contains("MEDIC") || titulo.Contains("CLINIC"))
                ? GradoEspecialistaSalud
                : GradoEspecialista;
        else if (titulo.Contains("DIPLOMADO") || titulo.Contains("DIPLOMA SUPERIOR"))
            gradoNombre = GradoDiplomaSuperior;

        int? gradoId = gradoNombre is not null ? grados.GetValueOrDefault(gradoNombre) : null;
        if (gradoId is null or 0)
        {
            _logger.LogWarning(
                "Grado de Cuarto Nivel no reconocido en título '{Titulo}' — va a revisión manual.", nombreTitulo);
            return new EducationLevelClassificationResult
            {
                EducationLevelTypeId = nivelId,
                RequiresManualReview = true,
                ReviewReason = $"No se pudo determinar el grado (Doctorado/Maestría/Especialista/Diplomado) del título \"{nombreTitulo}\"",
            };
        }

        return new EducationLevelClassificationResult { EducationLevelTypeId = nivelId, SiiesGradoTypeId = gradoId };
    }

    private async Task<(int? Nivel1Fallback, int? Nivel3, int? Nivel4, Dictionary<string, int> Grados)> GetCatalogAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKey, out (int? Nivel1Fallback, int? Nivel3, int? Nivel4, Dictionary<string, int> Grados) cached))
            return cached;

        var academicLevels = (await _refTypes.GetByCategoryAsync(AcademicLevelCategory, ct)).ToList();
        var nivel1 = academicLevels.FirstOrDefault(t => t.Name == "NIVEL_1")?.TypeId;
        var nivel3 = academicLevels.FirstOrDefault(t => t.Name == "NIVEL_3")?.TypeId;
        var nivel4 = academicLevels.FirstOrDefault(t => t.Name == "NIVEL_4")?.TypeId;

        var grados = (await _refTypes.GetByCategoryAsync(SiiesGradoCategory, ct))
            .Where(t => t.IsActive)
            .ToDictionary(t => t.Name, t => t.TypeId);

        var result = (nivel1, nivel3, nivel4, grados);
        _cache.Set(CacheKey, result, TimeSpan.FromMinutes(30));
        return result;
    }
}

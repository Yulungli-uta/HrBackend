using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WsUtaSystem.Application.DTOs.Dinardap;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class EducationLevelSyncService : IEducationLevelSyncService
{
    private readonly IHrDinardapClient _dinardap;
    private readonly IPeopleService _people;
    private readonly IEmployeesRepository _employees;
    private readonly IEducationLevelsService _educationLevels;
    private readonly IInstitutionsRepository _institutions;
    private readonly IEducationLevelClassifierService _classifier;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EducationLevelSyncService> _logger;

    // [2026-09-16] Preview y Confirm consultaban DINARDAP 2 veces seguidas para la misma
    // persona (una al abrir el diálogo, otra al confirmar) — corregido cacheando el resultado
    // del preview por 5 min; Confirm lo reutiliza en vez de volver a pegarle al servicio.
    // 5 min alcanza de sobra para que alguien revise el diálogo y decida, sin arrastrar un
    // preview viejo si tarda mucho más que eso (en ese caso simplemente vuelve a consultar).
    private const int PreviewCacheMinutes = 5;
    private static string PreviewCacheKey(int personId) => $"TituloPreview_{personId}";

    public EducationLevelSyncService(
        IHrDinardapClient dinardap,
        IPeopleService people,
        IEmployeesRepository employees,
        IEducationLevelsService educationLevels,
        IInstitutionsRepository institutions,
        IEducationLevelClassifierService classifier,
        IMemoryCache cache,
        ILogger<EducationLevelSyncService> logger)
    {
        _dinardap = dinardap;
        _people = people;
        _employees = employees;
        _educationLevels = educationLevels;
        _institutions = institutions;
        _classifier = classifier;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TituloSyncPreviewDto> PreviewAsync(int personId, CancellationToken ct = default)
    {
        var persona = await _people.GetByIdAsync(personId, ct)
            ?? throw new KeyNotFoundException($"Persona {personId} no encontrada.");

        var titulos = await _dinardap.ConsultarTitulosAsync(persona.IdCard, ct);
        _cache.Set(PreviewCacheKey(personId), titulos, TimeSpan.FromMinutes(PreviewCacheMinutes));
        var existentes = await ExistingRegistrationNumbersAsync(personId, ct);

        var items = new List<TituloSyncPreviewItemDto>();
        foreach (var t in titulos)
        {
            var yaExiste = !string.IsNullOrWhiteSpace(t.NumeroRegistro) && existentes.Contains(t.NumeroRegistro!);
            var clasif = yaExiste
                ? null
                : await _classifier.ClassifyAsync(t.NivelNombre, t.NombreTitulo, ct);

            items.Add(new TituloSyncPreviewItemDto(
                t.NumeroRegistro, t.NombreTitulo ?? "(sin nombre)", t.InstitucionEducacionSuperior,
                t.NivelNombre, yaExiste, clasif?.RequiresManualReview ?? false, clasif?.ReviewReason));
        }

        return new TituloSyncPreviewDto(
            personId, items.Count, items.Count(i => !i.YaExiste), items.Count(i => i.YaExiste), items);
    }

    public async Task<TituloSyncResultDto> ConfirmAsync(int personId, CancellationToken ct = default)
    {
        var persona = await _people.GetByIdAsync(personId, ct)
            ?? throw new KeyNotFoundException($"Persona {personId} no encontrada.");

        var (creados, omitidos, revision) = await SyncPersonAsync(personId, persona.IdCard, ct);
        return new TituloSyncResultDto(personId, creados, omitidos, revision);
    }

    public async Task<BulkTituloSyncResultDto> SyncActiveEmployeesAsync(CancellationToken ct = default)
    {
        var activos = await _employees.Query()
            .Where(e => e.IsActive)
            .Select(e => e.PersonID)
            .Distinct()
            .ToListAsync(ct);

        int procesadas = 0, conError = 0, creadosTotal = 0, omitidosTotal = 0;

        foreach (var personId in activos)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var persona = await _people.GetByIdAsync(personId, ct);
                if (persona is null || string.IsNullOrWhiteSpace(persona.IdCard))
                {
                    conError++;
                    continue;
                }

                var (creados, omitidos, _) = await SyncPersonAsync(personId, persona.IdCard, ct);
                creadosTotal += creados;
                omitidosTotal += omitidos;
                procesadas++;
            }
            catch (Exception ex)
            {
                // Omitir y continuar - un fallo puntual (DINARDAP caído para esta persona,
                // timeout, etc.) no debe abortar el lote completo. Decisión del usuario 2026-09-16.
                _logger.LogWarning(ex, "Falló la sincronización SENESCYT para PersonId={PersonId} — se omite y continúa.", personId);
                conError++;
            }
        }

        return new BulkTituloSyncResultDto(procesadas, conError, creadosTotal, omitidosTotal);
    }

    private async Task<(int Creados, int Omitidos, int RequierenRevision)> SyncPersonAsync(
        int personId, string identificacion, CancellationToken ct)
    {
        // Reusa el preview cacheado si Confirm llega poco después (flujo normal del diálogo);
        // si no hay nada cacheado (llamado directo, cache expirado, o el job masivo — que nunca
        // pasa por Preview) simplemente consulta DINARDAP como antes.
        IReadOnlyList<DinardapTituloDto> titulos;
        var cacheKey = PreviewCacheKey(personId);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<DinardapTituloDto>? cached) && cached is not null)
        {
            titulos = cached;
            _cache.Remove(cacheKey); // uso único — no reutilizar en una segunda confirmación posterior
        }
        else
        {
            titulos = await _dinardap.ConsultarTitulosAsync(identificacion, ct);
        }

        if (titulos.Count == 0) return (0, 0, 0);

        var existentes = await ExistingRegistrationNumbersAsync(personId, ct);
        int creados = 0, omitidos = 0, revision = 0;

        foreach (var t in titulos)
        {
            if (!string.IsNullOrWhiteSpace(t.NumeroRegistro) && existentes.Contains(t.NumeroRegistro!))
            {
                omitidos++;
                continue;
            }

            var clasif = await _classifier.ClassifyAsync(t.NivelNombre, t.NombreTitulo, ct);
            if (clasif.EducationLevelTypeId is null)
            {
                // Solo pasa si NIVEL_1 tampoco existe en el catálogo (config rota) - no hay
                // ningún EducationLevelTypeId válido para insertar, se omite este título.
                _logger.LogError("No hay ningún ACADEMIC_LEVEL válido para clasificar el título de PersonId={PersonId} - revisar catálogo.", personId);
                revision++;
                continue;
            }

            var institutionId = await ResolveInstitutionIdAsync(t.InstitucionEducacionSuperior, ct);

            var entity = new EducationLevels
            {
                PersonId = personId,
                EducationLevelTypeId = clasif.EducationLevelTypeId.Value,
                InstitutionId = institutionId,
                InstitutionNameOriginal = t.InstitucionEducacionSuperior,
                Title = t.NombreTitulo ?? "(sin nombre)",
                SenescytRegistrationNumber = t.NumeroRegistro,
                SiiesGradoTypeId = clasif.SiiesGradoTypeId,
                SenescytGraduationDate = t.FechaGrado.HasValue ? DateOnly.FromDateTime(t.FechaGrado.Value) : null,
                SenescytRegistrationDate = t.FechaRegistro.HasValue ? DateOnly.FromDateTime(t.FechaRegistro.Value) : null,
                SenescytType = t.Tipo,
                SenescytNivelNombreOriginal = t.NivelNombre,
                Source = "Dinardap",
            };

            await _educationLevels.CreateAsync(entity, ct);
            creados++;
            if (clasif.RequiresManualReview) revision++;
        }

        return (creados, omitidos, revision);
    }

    private async Task<HashSet<string>> ExistingRegistrationNumbersAsync(int personId, CancellationToken ct)
    {
        var existentes = await _educationLevels.GetByPersonIdAsync(personId);
        return existentes
            .Where(e => !string.IsNullOrWhiteSpace(e.SenescytRegistrationNumber))
            .Select(e => e.SenescytRegistrationNumber!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Busca una institución existente por nombre exacto (sin distinguir mayúsculas). Si no
    /// coincide con ninguna, retorna null - el nombre real igual se guarda en
    /// InstitutionNameOriginal (ver EducationLevels.InstitutionId). No se crea una institución
    /// nueva automáticamente: HR.tbl_Institutions exige Tipo/País/Provincia/Cantón, dato que
    /// DINARDAP no manda para universidades extranjeras (decisión del usuario 2026-09-16).
    /// </summary>
    private async Task<int?> ResolveInstitutionIdAsync(string? nombreInstitucion, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nombreInstitucion)) return null;

        var match = await _institutions.Query()
            .Where(i => i.Name.ToUpper() == nombreInstitucion.ToUpper())
            .Select(i => (int?)i.InstitutionId)
            .FirstOrDefaultAsync(ct);

        return match;
    }
}

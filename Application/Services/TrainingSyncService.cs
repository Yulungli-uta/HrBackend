using System.Globalization;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.Trainings;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services;

public sealed class TrainingSyncService : ITrainingSyncService
{
    private const string SourceDeadv = "DEaDV";
    private const string DefaultCountryId = "001"; // ECUADOR
    private const string InstitutionName = "Universidad Técnica de Ambato";
    private const int PreviewCacheMinutes = 5;

    private readonly IDeadvClient _deadv;
    private readonly IPeopleService _people;
    private readonly ITrainingsService _trainings;
    private readonly ITrainingsRepository _trainingsRepo;
    private readonly IRefTypesRepository _refTypes;
    private readonly IMemoryCache _cache;

    public TrainingSyncService(
        IDeadvClient deadv,
        IPeopleService people,
        ITrainingsService trainings,
        ITrainingsRepository trainingsRepo,
        IRefTypesRepository refTypes,
        IMemoryCache cache)
    {
        _deadv = deadv;
        _people = people;
        _trainings = trainings;
        _trainingsRepo = trainingsRepo;
        _refTypes = refTypes;
        _cache = cache;
    }

    private static string PreviewCacheKey(int personId) => $"DeadvPreview_{personId}";

    public async Task<DeadvSyncPreviewDto> PreviewAsync(int personId, CancellationToken ct = default)
    {
        var certs = await _deadv.GetCertificatesAsync(await GetIdCardAsync(personId, ct), ct);
        if (certs is null)
            return new DeadvSyncPreviewDto(personId, false, 0, 0, 0, 0, Array.Empty<DeadvSyncPreviewItemDto>());

        // Confirm reutiliza esta consulta en vez de pegarle otra vez a DEaDV (uso único, 5 min).
        _cache.Set(PreviewCacheKey(personId), certs, TimeSpan.FromMinutes(PreviewCacheMinutes));

        var plan = BuildPlan(certs, (await _trainingsRepo.GetByPersonIdAsync(personId)).ToList());
        var items = plan.Select(p => new DeadvSyncPreviewItemDto(
            p.Cert.Code, p.Cert.EventName, p.Cert.Area, IsPedagogical(p.Cert), p.Cert.StartDate, p.Cert.EndDate,
            p.Cert.Hours, p.Cert.Modality, p.Status)).ToList();

        return new DeadvSyncPreviewDto(
            personId, true, items.Count,
            items.Count(i => i.Status == DeadvSyncStatus.New),
            items.Count(i => i.Status == DeadvSyncStatus.Linkable),
            items.Count(i => i.Status == DeadvSyncStatus.AlreadySynced),
            items);
    }

    public async Task<DeadvSyncResultDto> ConfirmAsync(int personId, CancellationToken ct = default)
    {
        IReadOnlyList<DeadvCertificateDto>? certs;
        var cacheKey = PreviewCacheKey(personId);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<DeadvCertificateDto>? cached) && cached is not null)
        {
            certs = cached;
            _cache.Remove(cacheKey);
        }
        else
        {
            certs = await _deadv.GetCertificatesAsync(await GetIdCardAsync(personId, ct), ct);
        }
        if (certs is null)
            throw new BusinessRuleException("No se pudo consultar DEaDV en este momento. Intente nuevamente más tarde.");

        var existing = (await _trainingsRepo.GetByPersonIdAsync(personId)).ToList();
        var plan = BuildPlan(certs, existing);
        if (plan.All(p => p.Status == DeadvSyncStatus.AlreadySynced)) return new DeadvSyncResultDto(personId, 0, 0, plan.Count);

        var catalogs = await LoadCatalogsAsync(ct);
        int created = 0, linked = 0;

        foreach (var p in plan)
        {
            ct.ThrowIfCancellationRequested();
            if (p.Status == DeadvSyncStatus.New)
            {
                await _trainings.CreateAsync(NewTraining(personId, p.Cert, catalogs), ct);
                created++;
            }
            else if (p.Status == DeadvSyncStatus.Linkable && p.Legacy is not null)
            {
                await LinkAsync(p.Legacy.TrainingId, p.Cert, catalogs, ct);
                linked++;
            }
        }

        return new DeadvSyncResultDto(personId, created, linked, plan.Count - created - linked);
    }

    public Task<byte[]?> GetCertificatePdfAsync(Trainings training, CancellationToken ct = default) =>
        string.IsNullOrWhiteSpace(training.CertificateUrl)
            ? Task.FromResult<byte[]?>(null)
            : _deadv.DownloadCertificatePdfAsync(training.CertificateUrl, ct);

    private async Task<string> GetIdCardAsync(int personId, CancellationToken ct)
    {
        var person = await _people.GetByIdAsync(personId, ct)
            ?? throw new KeyNotFoundException($"Persona {personId} no encontrada.");
        if (string.IsNullOrWhiteSpace(person.IdCard))
            throw new BusinessRuleException("La persona no tiene número de identificación registrado.");
        return person.IdCard;
    }

    private sealed record PlanItem(DeadvCertificateDto Cert, string Status, Trainings? Legacy);

    /// <summary>
    /// Clasifica cada certificado: ya sincronizado (mismo ExternalId), vinculable (registro previo sin
    /// origen con la misma fecha de inicio y título equivalente; uno a uno) o nuevo.
    /// </summary>
    private static List<PlanItem> BuildPlan(IReadOnlyList<DeadvCertificateDto> certs, List<Trainings> existing)
    {
        var synced = existing
            .Where(t => !string.IsNullOrWhiteSpace(t.ExternalId))
            .Select(t => t.ExternalId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var legacy = existing.Where(t => string.IsNullOrWhiteSpace(t.ExternalId)).ToList();
        var used = new HashSet<int>();

        var plan = new List<PlanItem>();
        foreach (var cert in certs.GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
        {
            if (synced.Contains(cert.Code))
            {
                plan.Add(new PlanItem(cert, DeadvSyncStatus.AlreadySynced, null));
                continue;
            }

            var key = TitleKey(cert.EventName);
            var match = legacy.FirstOrDefault(t => !used.Contains(t.TrainingId)
                && t.StartDate == cert.StartDate && TitleKey(t.Title) == key);
            if (match is not null)
            {
                used.Add(match.TrainingId);
                plan.Add(new PlanItem(cert, DeadvSyncStatus.Linkable, match));
            }
            else
            {
                plan.Add(new PlanItem(cert, DeadvSyncStatus.New, null));
            }
        }
        return plan;
    }

    private Trainings NewTraining(int personId, DeadvCertificateDto cert, Catalogs cat) => new()
    {
        PersonId = personId,
        Title = Truncate(cert.EventName, 200)!,
        Institution = InstitutionName,
        CertifiedBy = Truncate(cert.Dependency, 150),
        StartDate = cert.StartDate,
        EndDate = cert.EndDate,
        Hours = cert.Hours,
        IsPedagogical = IsPedagogical(cert),
        EventTypeId = cat.EventType(cert.EventType),
        CertificateTypeId = cat.CertificateType(cert.Area),
        ApprovalTypeId = cat.Approval(cert.ApprovalType),
        TrainingDirectionTypeId = cat.Received,
        ModalityTypeId = cat.Modality(cert.Modality),
        CountryId = DefaultCountryId,
        Source = SourceDeadv,
        ExternalId = cert.Code,
        CertificateUrl = Truncate(cert.FileUrl, 500),
    };

    /// <summary>
    /// Completa un registro histórico con los datos de DEaDV SOLO donde está vacío o es inválido
    /// (horas en 0, catálogos nulos); nunca pisa lo que ya tiene un valor. Pasa por el repositorio
    /// porque TrainingsService.UpdateAsync preserva el origen y aquí el origen debe cambiar.
    /// </summary>
    private async Task LinkAsync(int trainingId, DeadvCertificateDto cert, Catalogs cat, CancellationToken ct)
    {
        var t = await _trainingsRepo.GetByIdAsync(trainingId, ct)
            ?? throw new KeyNotFoundException($"Capacitación {trainingId} no encontrada.");

        t.Source = SourceDeadv;
        t.ExternalId = cert.Code;
        t.CertificateUrl = Truncate(cert.FileUrl, 500);
        if (t.Hours <= 0) t.Hours = cert.Hours;
        t.IsPedagogical ??= IsPedagogical(cert);
        t.EventTypeId ??= cat.EventType(cert.EventType);
        t.CertificateTypeId ??= cat.CertificateType(cert.Area);
        t.ApprovalTypeId ??= cat.Approval(cert.ApprovalType);
        t.TrainingDirectionTypeId ??= cat.Received;
        t.ModalityTypeId ??= cat.Modality(cert.Modality);
        t.CountryId ??= DefaultCountryId;
        if (string.IsNullOrWhiteSpace(t.CertifiedBy)) t.CertifiedBy = Truncate(cert.Dependency, 150);
        if (t.IsPedagogical == true) t.KnowledgeAreaTypeId = null;

        await _trainingsRepo.UpdateAsync(trainingId, t, ct);
    }

    /// <summary>Regla de negocio: área DOCENCIA = pedagógica; cualquier otra = específica.</summary>
    private static bool IsPedagogical(DeadvCertificateDto cert) => Normalize(cert.Area) == "DOCENCIA";

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];

    /// <summary>Mayúsculas, sin tildes y espacios colapsados.</summary>
    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var ch in value.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        return string.Join(' ', sb.ToString().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Título comparable: normalizado y sin el prefijo "CURSO INTERSEMESTRAL:" que DEaDV agrega.</summary>
    private static string TitleKey(string? title)
    {
        var n = Normalize(title);
        var colon = n.IndexOf(':');
        return colon is > 0 and <= 30 ? n[(colon + 1)..].Trim() : n;
    }

    private async Task<Catalogs> LoadCatalogsAsync(CancellationToken ct) => new(
        await LoadAsync("EVENT_TYPE", ct),
        await LoadAsync("CERTIFICATION_TYPE", ct),
        await LoadAsync("CERT_APPROVAL_TYPE", ct),
        await LoadAsync("TRAINING_DIRECTION", ct),
        await LoadAsync("TRAINING_MODALITY", ct));

    // ref_Types.TypeID es IDENTITY y varía entre ambientes: siempre se resuelve por Name.
    private async Task<Dictionary<string, int>> LoadAsync(string category, CancellationToken ct)
    {
        var map = new Dictionary<string, int>();
        foreach (var r in await _refTypes.GetByCategoryAsync(category, ct))
            map.TryAdd(Normalize(r.Name), r.TypeId);
        return map;
    }

    private sealed class Catalogs
    {
        private readonly Dictionary<string, int> _event, _certificate, _approval, _direction, _modality;

        public Catalogs(Dictionary<string, int> ev, Dictionary<string, int> cert, Dictionary<string, int> approval,
            Dictionary<string, int> direction, Dictionary<string, int> modality)
        {
            _event = ev; _certificate = cert; _approval = approval; _direction = direction; _modality = modality;
        }

        public int? Received => Find(_direction, "CAPACITACION RECIBIDA");

        public int? EventType(string? v) => Find(_event, Normalize(v)) ?? Find(_event, "OTRO");

        public int? Approval(string? v) => Find(_approval, Normalize(v));

        public int? CertificateType(string? area) => Normalize(area) switch
        {
            "DOCENCIA" => Find(_certificate, "CURSO EN EL CAMPO DE DOCENCIA UNIVERSITARIA"),
            "ESPECIFICAS" => Find(_certificate, "ACTUALIZACION Y PERFECCIONAMIENTO EN EL CAMPO ESPECIFICO"),
            _ => Find(_certificate, "OTROS CERTIFICADOS"),
        };

        public int? Modality(string? v) => Normalize(v) switch
        {
            "VIRTUAL" => Find(_modality, "EN LINEA"),
            "PRESENCIAL" => Find(_modality, "PRESENCIAL"),
            "B-LEARNING" => Find(_modality, "HIBRIDA"),
            _ => null,
        };

        private static int? Find(Dictionary<string, int> map, string key) => map.TryGetValue(key, out var id) ? id : null;
    }
}

using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Guards;
using WsUtaSystem.Application.Interfaces.Guards;
using WsUtaSystem.Data;
using WsUtaSystem.Models;
using WsUtaSystem.Models.Guards;

namespace WsUtaSystem.Application.Services.Guards;

public class RotationPatternService : IRotationPatternService
{
    private readonly IRotationPatternRepository _repo;
    private readonly AppDbContext _db;

    public RotationPatternService(IRotationPatternRepository repo, AppDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task<List<RotationPatternDto>> GetAllAsync(CancellationToken ct)
    {
        var patterns = await _db.RotationPatterns
            .Include(p => p.PatternType)
            .Include(p => p.Details.OrderBy(d => d.DayOrder)).ThenInclude(d => d.Schedule)
            .Where(p => p.IsActive)
            .ToListAsync(ct);
        return patterns.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<RotationPatternDto>> GetPagedAsync(int page, int pageSize, string? search, bool? isActive, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var baseQ = _db.RotationPatterns.AsQueryable();
        if (isActive.HasValue)
            baseQ = baseQ.Where(p => p.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            baseQ = baseQ.Where(p => p.Name.ToLower().Contains(term) ||
                                     (p.PatternCode != null && p.PatternCode.ToLower().Contains(term)));
        }

        var total = await baseQ.LongCountAsync(ct);
        var items = await baseQ
            .Include(p => p.PatternType)
            .Include(p => p.Details.OrderBy(d => d.DayOrder)).ThenInclude(d => d.Schedule)
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<RotationPatternDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<RotationPatternDto?> GetByIdAsync(int patternId, CancellationToken ct)
    {
        var p = await _repo.GetWithDetailsAsync(patternId, ct);
        return p is null ? null : MapToDto(p);
    }

    public async Task<RotationPatternDto> CreateAsync(CreateRotationPatternDto dto, CancellationToken ct)
    {
        await ValidateDetailsCoverageAsync(dto.CycleDays, dto.Details, ct);

        await EnsurePatternIsNotDuplicatedAsync(null, dto.PatternCode, dto.Name, dto.CycleDays, dto.Details, ct);

        var entity = new RotationPattern
        {
            PatternCode = dto.PatternCode,
            Name = dto.Name,
            Description = dto.Description,
            PatternTypeId = dto.PatternTypeId,
            CycleDays = dto.CycleDays,
            IsActive = true
        };

        foreach (var d in dto.Details)
        {
            entity.Details.Add(new RotationPatternDetail
            {
                DayOrder = d.DayOrder,
                ScheduleId = d.ScheduleId,
                IsRestDay = d.IsRestDay,
                Notes = d.Notes
            });
        }

        await _db.RotationPatterns.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<RotationPatternDto> UpdateAsync(int patternId, UpdateRotationPatternDto dto, CancellationToken ct)
    {
        var entity = await _db.RotationPatterns.FirstOrDefaultAsync(p => p.PatternId == patternId, ct)
            ?? throw new KeyNotFoundException($"Patrón {patternId} no encontrado.");
        await EnsurePatternHeaderIsNotDuplicatedAsync(patternId, dto.PatternCode, dto.Name, ct);
        entity.PatternCode = dto.PatternCode;
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<RotationPatternDto> SetDetailsAsync(int patternId, UpsertRotationPatternDetailsDto dto, CancellationToken ct)
    {
        var entity = await _repo.GetWithDetailsAsync(patternId, ct)
            ?? throw new KeyNotFoundException($"Patrón {patternId} no encontrado.");

        await ValidateDetailsCoverageAsync(entity.CycleDays, dto.Details, ct);

        await EnsurePatternIsNotDuplicatedAsync(patternId, entity.PatternCode, entity.Name, entity.CycleDays, dto.Details, ct);

        _db.RotationPatternDetails.RemoveRange(entity.Details);

        foreach (var d in dto.Details)
        {
            entity.Details.Add(new RotationPatternDetail
            {
                PatternId = patternId,
                DayOrder = d.DayOrder,
                ScheduleId = d.ScheduleId,
                IsRestDay = d.IsRestDay,
                Notes = d.Notes
            });
        }

        await _db.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    private static RotationPatternDto MapToDto(RotationPattern p) =>
        new(p.PatternId, p.PatternCode, p.Name, p.Description,
            p.PatternTypeId, p.PatternType?.Name, p.CycleDays, p.IsActive,
            p.Details.OrderBy(d => d.DayOrder).Select(d => new RotationPatternDetailDto(
                d.PatternDetailId, d.PatternId, d.DayOrder, d.ScheduleId,
                d.Schedule?.Description, d.Schedule?.ScheduleCode, d.IsRestDay, d.Notes
            )).ToList());

    private async Task EnsurePatternHeaderIsNotDuplicatedAsync(
        int? currentPatternId,
        string? patternCode,
        string name,
        CancellationToken ct)
    {
        var normalizedCode = Normalize(patternCode);
        var normalizedName = Normalize(name);

        if (!string.IsNullOrEmpty(normalizedCode))
        {
            var codeExists = await _db.RotationPatterns.AnyAsync(p =>
                (!currentPatternId.HasValue || p.PatternId != currentPatternId.Value)
                && p.PatternCode != null
                && p.PatternCode.Trim().ToLower() == normalizedCode, ct);

            if (codeExists)
                throw new InvalidOperationException($"Ya existe un patron de rotacion con el codigo '{patternCode}'.");
        }

        var nameExists = await _db.RotationPatterns.AnyAsync(p =>
            (!currentPatternId.HasValue || p.PatternId != currentPatternId.Value)
            && p.Name.Trim().ToLower() == normalizedName, ct);

        if (nameExists)
            throw new InvalidOperationException($"Ya existe un patron de rotacion con el nombre '{name}'.");
    }

    private async Task EnsurePatternIsNotDuplicatedAsync(
        int? currentPatternId,
        string? patternCode,
        string name,
        int cycleDays,
        IReadOnlyCollection<CreateRotationPatternDetailDto> details,
        CancellationToken ct)
    {
        await EnsurePatternHeaderIsNotDuplicatedAsync(currentPatternId, patternCode, name, ct);

        var incomingSignature = BuildDetailsSignature(details);
        var candidates = await _db.RotationPatterns
            .Include(p => p.Details)
            .Where(p => p.IsActive
                        && p.CycleDays == cycleDays
                        && (!currentPatternId.HasValue || p.PatternId != currentPatternId.Value))
            .ToListAsync(ct);

        var duplicate = candidates.FirstOrDefault(p => BuildDetailsSignature(p.Details.Select(d =>
            new CreateRotationPatternDetailDto(d.DayOrder, d.ScheduleId, d.IsRestDay, d.Notes)).ToList()) == incomingSignature);

        if (duplicate is not null)
            throw new InvalidOperationException($"Ya existe un patron de rotacion activo con la misma secuencia: '{duplicate.Name}'.");
    }

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLower();

    private static string BuildDetailsSignature(IEnumerable<CreateRotationPatternDetailDto> details) =>
        string.Join("|", details
            .OrderBy(d => d.DayOrder).ThenBy(d => d.ScheduleId)
            .Select(d => $"{d.DayOrder}:{d.ScheduleId?.ToString() ?? "REST"}:{d.IsRestDay}"));

    // Valida que el patrón cubra exactamente las posiciones 1..CycleDays (cada una con 1 o más
    // horarios) y que, cuando un día tiene varios horarios en paralelo, estos no queden pegados
    // ni se encimen entre sí (ver hallazgo: "agregar un turno más en el mismo día del patrón").
    private async Task ValidateDetailsCoverageAsync(
        int cycleDays, IReadOnlyCollection<CreateRotationPatternDetailDto> details, CancellationToken ct)
    {
        var byDay = details.GroupBy(d => d.DayOrder).ToDictionary(g => g.Key, g => g.ToList());

        var missing = Enumerable.Range(1, cycleDays).Where(day => !byDay.ContainsKey(day)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"Faltan horarios para el/los día(s) del ciclo: {string.Join(", ", missing)}.");

        var outOfRange = byDay.Keys.Where(day => day < 1 || day > cycleDays).ToList();
        if (outOfRange.Count > 0)
            throw new InvalidOperationException($"Día(s) de ciclo fuera de rango (1-{cycleDays}): {string.Join(", ", outOfRange)}.");

        var scheduleIds = new List<int>();

        foreach (var (day, rows) in byDay)
        {
            if (rows.Count > 1)
            {
                if (rows.Any(r => r.IsRestDay))
                    throw new InvalidOperationException($"El día {day} no puede combinar descanso con horarios de trabajo.");

                if (rows.Any(r => r.ScheduleId is null))
                    throw new InvalidOperationException($"El día {day} tiene un horario sin definir.");

                if (rows.Select(r => r.ScheduleId).Distinct().Count() != rows.Count)
                    throw new InvalidOperationException($"El día {day} tiene el mismo horario repetido.");
            }

            scheduleIds.AddRange(rows.Where(r => r.ScheduleId.HasValue).Select(r => r.ScheduleId!.Value));
        }

        if (scheduleIds.Count == 0) return;

        var schedules = await _db.Set<Schedules>()
            .Where(s => scheduleIds.Contains(s.ScheduleId))
            .ToDictionaryAsync(s => s.ScheduleId, s => s, ct);

        foreach (var (day, rows) in byDay)
        {
            var daySchedules = rows.Where(r => r.ScheduleId.HasValue).Select(r => schedules[r.ScheduleId!.Value]).ToList();
            for (var i = 0; i < daySchedules.Count; i++)
                for (var j = i + 1; j < daySchedules.Count; j++)
                    if (AreBackToBackOrOverlapping(daySchedules[i], daySchedules[j]))
                        throw new InvalidOperationException(
                            $"El día {day} tiene horarios consecutivos o encimados ({daySchedules[i].ScheduleCode} / {daySchedules[j].ScheduleCode}). " +
                            "Los turnos del mismo día deben dejar un descanso entre ellos.");
        }
    }

    // Compara dos horarios del MISMO día del patrón como rangos en minutos desde las 00:00 de
    // ese día (un turno que cruza medianoche se representa extendido más allá de 1440, ej.
    // 23:00-07:30 = [1380, 1890)). No se debe "envolver" la comparación a ±1440: ambos horarios
    // arrancan el mismo día de calendario, así que comparar el rango tal cual ya es correcto —
    // envolver generaba falsos positivos (ej. Mañana 07-15:30 vs Noche 23-07:30 detectados como
    // encimados por comparar la cola de una Noche de OTRO día contra la Mañana de este día).
    private static bool AreBackToBackOrOverlapping(Schedules a, Schedules b)
    {
        var (aStart, aEnd) = ToMinuteRange(a);
        var (bStart, bEnd) = ToMinuteRange(b);

        if (bStart < aEnd && bEnd > aStart) return true;
        if (bStart == aEnd || bEnd == aStart) return true;

        return false;
    }

    private static (int start, int end) ToMinuteRange(Schedules s)
    {
        var start = s.EntryTime.Hour * 60 + s.EntryTime.Minute;
        var durationMinutes = ((s.ExitTime.Hour * 60 + s.ExitTime.Minute) - start + 1440) % 1440;
        if (durationMinutes == 0) durationMinutes = 1440;
        return (start, start + durationMinutes);
    }
}

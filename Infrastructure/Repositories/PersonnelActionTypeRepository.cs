using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.DTOs.PersonnelActionType;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public sealed class PersonnelActionTypeRepository
    : ServiceAwareEfRepository<PersonnelActionType, int>, IPersonnelActionTypeRepository
{
    public PersonnelActionTypeRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<List<PersonnelActionType>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.Set<PersonnelActionType>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

    /// <inheritdoc/>
    public async Task<Application.DTOs.Common.PagedResult<PersonnelActionTypeFamily>> GetFamiliesPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        const string Collation = "Latin1_General_100_CI_AI"; // sin distinguir mayúsculas ni acentos
        var all = _db.Set<PersonnelActionType>().AsNoTracking();

        // Tipos que coinciden con la búsqueda: cada palabra debe aparecer en nombre, código, prefijo o descripción.
        var terms = (search ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IQueryable<PersonnelActionType> matched = all;
        foreach (var raw in terms)
        {
            var term = raw;
            matched = matched.Where(t =>
                EF.Functions.Collate(t.Name, Collation).Contains(term) ||
                EF.Functions.Collate(t.Code, Collation).Contains(term) ||
                EF.Functions.Collate(t.NumberingPrefix, Collation).Contains(term) ||
                (t.Description != null && EF.Functions.Collate(t.Description, Collation).Contains(term)));
        }
        var matchedIds = matched.Select(t => t.PersonnelActionTypeId);

        // Tipos generales (sin padre) que coinciden o con algún subtipo que coincide.
        var roots = all.Where(t => t.ParentTypeId == null);
        if (terms.Length > 0)
            roots = roots.Where(t =>
                matchedIds.Contains(t.PersonnelActionTypeId) ||
                all.Any(c => c.ParentTypeId == t.PersonnelActionTypeId && matchedIds.Contains(c.PersonnelActionTypeId)));

        var total = await roots.LongCountAsync(ct);
        if (total == 0)
            return Application.DTOs.Common.PagedResult<PersonnelActionTypeFamily>.Empty(page, pageSize);

        var pageRoots = await roots
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        var rootIds = pageRoots.Select(r => r.PersonnelActionTypeId).ToList();

        var children = await all
            .Where(c => c.ParentTypeId != null && rootIds.Contains(c.ParentTypeId.Value))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        // Ids que coinciden con la búsqueda dentro de esta página (para decidir qué subtipos mostrar).
        HashSet<int> matchedInPage = [];
        if (terms.Length > 0)
        {
            var pageIds = rootIds.Concat(children.Select(c => c.PersonnelActionTypeId)).ToList();
            matchedInPage = (await matchedIds.Where(id => pageIds.Contains(id)).ToListAsync(ct)).ToHashSet();
        }

        var childrenByParent = children.GroupBy(c => c.ParentTypeId!.Value).ToDictionary(g => g.Key, g => g.ToList());
        var families = pageRoots.Select(root =>
        {
            var kids = childrenByParent.GetValueOrDefault(root.PersonnelActionTypeId) ?? [];
            var visible = terms.Length == 0 || matchedInPage.Contains(root.PersonnelActionTypeId)
                ? kids
                : kids.Where(k => matchedInPage.Contains(k.PersonnelActionTypeId)).ToList();
            return new PersonnelActionTypeFamily(root, visible, kids.Count);
        }).ToList();

        return new Application.DTOs.Common.PagedResult<PersonnelActionTypeFamily>
        {
            Items = families,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task SetDefaultTemplateAsync(int personnelActionTypeId, int? templateId, CancellationToken ct = default)
    {
        var entity = await _db.Set<PersonnelActionType>()
            .FirstOrDefaultAsync(x => x.PersonnelActionTypeId == personnelActionTypeId, ct)
            ?? throw new KeyNotFoundException($"PersonnelActionType id={personnelActionTypeId} no existe.");

        entity.DefaultTemplateId = templateId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(string DocumentNumber, int Year, int Sequence)> ConsumeNextNumberAsync(
        int personnelActionTypeId,
        int year,
        CancellationToken ct = default)
    {
        // UPDATE atómico con OUTPUT: evita el bug de entity tracker + HasDefaultValue
        // + MARS que causaba duplicados en UQ_PersonnelActions_ActionNumber.
        // No requiere transacción explícita: el UPDATE de una sola fila es atómico en SQL Server.
        // 2026-11-11: NumberingResetsYearly = 0 (series de licencia del legado) hace la secuencia acumulada:
        // el año solo es un sufijo del número y no la reinicia. Con el valor por defecto (1) el comportamiento
        // es el de siempre.
        const string sql = """
            UPDATE HR.tbl_Personnel_Action_Type
            SET   NumberingLastSequence = CASE
                      WHEN NumberingResetsYearly = 0 OR NumberingYear = @year THEN NumberingLastSequence + 1
                      ELSE 1
                  END,
                  NumberingYear = @year,
                  UpdatedAt     = SYSDATETIME()
            OUTPUT INSERTED.NumberingPrefix, INSERTED.NumberingLastSequence, INSERTED.NumberingFormat
            WHERE  PersonnelActionTypeID = @id
            """;

        var conn = (SqlConnection)_db.Database.GetDbConnection();
        bool opened = conn.State != System.Data.ConnectionState.Open;
        if (opened) await conn.OpenAsync(ct);

        try
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", personnelActionTypeId);
            cmd.Parameters.AddWithValue("@year", year);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                throw new KeyNotFoundException(
                    $"PersonnelActionType id={personnelActionTypeId} no existe.");

            var prefix   = reader.GetString(0);
            var sequence = reader.GetInt32(1);
            var format   = reader.IsDBNull(2) ? null : reader.GetString(2);
            return (FormatDocumentNumber(prefix, year, sequence, format), year, sequence);
        }
        finally
        {
            if (opened) conn.Close();
        }
    }

    /// <summary>
    /// Arma el número del documento. Sin formato: "{prefijo}-{año}-{secuencia:D3}" (ej. DAP-2026-001).
    /// Con formato: reemplaza los tokens {PREFIX}, {SEQ} (sin ceros a la izquierda) y {YEAR}
    /// (ej. "DTH-UTA-{PREFIX}-{SEQ}-{YEAR}" da "DTH-UTA-LPE-2057-2026").
    /// </summary>
    public static string FormatDocumentNumber(string prefix, int year, int sequence, string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return $"{prefix}-{year}-{sequence:D3}";

        return format
            .Replace("{PREFIX}", prefix, StringComparison.Ordinal)
            .Replace("{SEQ}", sequence.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{YEAR}", year.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    public async Task<bool> HasChildrenAsync(int personnelActionTypeId, CancellationToken ct = default)
        => await _db.Set<PersonnelActionType>()
            .AsNoTracking()
            .AnyAsync(x => x.ParentTypeId == personnelActionTypeId, ct);

    public async Task<int?> GetRootTypeIdAsync(int personnelActionTypeId, CancellationToken ct = default)
    {
        var row = await _db.Set<PersonnelActionType>()
            .AsNoTracking()
            .Where(x => x.PersonnelActionTypeId == personnelActionTypeId)
            .Select(x => new { x.PersonnelActionTypeId, x.ParentTypeId })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : row.ParentTypeId ?? row.PersonnelActionTypeId;
    }

    public async Task UpdateSettingsAsync(
        int personnelActionTypeId, Action<PersonnelActionType> apply, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(apply);

        var entity = await _db.Set<PersonnelActionType>()
            .FirstOrDefaultAsync(x => x.PersonnelActionTypeId == personnelActionTypeId, ct)
            ?? throw new KeyNotFoundException($"PersonnelActionType id={personnelActionTypeId} no existe.");

        apply(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<LeaveActionContext> GetLeaveContextAsync(
        int personnelActionTypeId, int? leaveReasonId, CancellationToken ct = default)
    {
        var type = await _db.Set<PersonnelActionType>()
            .AsNoTracking()
            .Where(x => x.PersonnelActionTypeId == personnelActionTypeId)
            .Select(x => new { x.IsActive, x.Code, x.NumberingPrefix, x.ParentTypeId })
            .FirstOrDefaultAsync(ct);

        if (type is null)
            return LeaveActionContext.NotFound;

        // Solo cuentan los hijos ACTIVOS: mientras las series estén inactivas (antes del corte con el legado) el tipo
        // general sigue admitiendo acciones nuevas, como hasta ahora.
        var hasChildren = await _db.Set<PersonnelActionType>()
            .AsNoTracking()
            .AnyAsync(x => x.ParentTypeId == personnelActionTypeId && x.IsActive, ct);

        // Los motivos activos de la serie del tipo: el catálogo declara en Metadata.types a qué series aplica.
        var reasons = await _db.Set<RefTypes>()
            .AsNoTracking()
            .Where(r => r.Category == LeaveReasonCategory && r.IsActive)
            .Select(r => new { r.TypeId, r.Metadata })
            .ToListAsync(ct);

        var reasonIds = reasons
            .Where(r => ReasonAppliesToSeries(r.Metadata, type.NumberingPrefix))
            .Select(r => r.TypeId)
            .ToHashSet();

        return new LeaveActionContext(
            TypeExists: true,
            TypeIsActive: type.IsActive,
            TypeHasChildren: hasChildren,
            ParentTypeId: type.ParentTypeId,
            TypeCode: type.Code,
            ReasonsAvailable: reasonIds.Count > 0,
            ReasonProvided: leaveReasonId.HasValue,
            ReasonValid: leaveReasonId.HasValue && reasonIds.Contains(leaveReasonId.Value));
    }

    /// <summary>Categoría de ref_Types con los motivos de licencia.</summary>
    internal const string LeaveReasonCategory = "PERSONNEL_ACTION_LEAVE_REASON";

    /// <summary>
    /// Indica si un motivo aplica a la serie (prefijo) dada, según el arreglo "types" de su Metadata JSON
    /// (ej. {"types":["LCR","LCRA"]}). Un Metadata ausente o ilegible no aplica a ninguna serie.
    /// </summary>
    internal static bool ReasonAppliesToSeries(string? metadata, string? seriesPrefix)
    {
        if (string.IsNullOrWhiteSpace(metadata) || string.IsNullOrWhiteSpace(seriesPrefix))
            return false;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(metadata);
            if (!doc.RootElement.TryGetProperty("types", out var types)
                || types.ValueKind != System.Text.Json.JsonValueKind.Array)
                return false;

            foreach (var item in types.EnumerateArray())
                if (item.ValueKind == System.Text.Json.JsonValueKind.String
                    && string.Equals(item.GetString(), seriesPrefix, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}

using Dapper;
using Microsoft.Data.SqlClient;
using WsUtaSystem.Application.Common;
using WsUtaSystem.Application.DTOs.Common;
using WsUtaSystem.Application.DTOs.Dinardap;
using WsUtaSystem.Application.Interfaces.Services;

namespace WsUtaSystem.Infrastructure.Services;

/// <summary>
/// Implementación de <see cref="IDinardapQueryAuditService"/> vía Dapper contra HR.tbl_DinardapQueryLog. Es infraestructura
/// técnica de auditoría (mismo criterio que <see cref="JobExecutionLogService"/>): abre su propia conexión, así que escribir
/// el registro no confirma ni descarta cambios pendientes del DbContext de quien llama.
/// </summary>
public sealed class DinardapQueryAuditService : IDinardapQueryAuditService
{
    private const int MaxPageSize = 100;

    private readonly string _connectionString;
    private readonly ILogger<DinardapQueryAuditService> _logger;

    public DinardapQueryAuditService(IConfiguration configuration, ILogger<DinardapQueryAuditService> logger)
    {
        _connectionString = configuration.GetConnectionString("SqlServerConn")
            ?? throw new InvalidOperationException("Connection string 'SqlServerConn' not found.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task RecordAsync(DinardapQueryRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        try
        {
            const string sql = @"
INSERT INTO HR.tbl_DinardapQueryLog
    (Origin, UserId, UserEmail, EmployeeId, Package, IdentificationMasked, Outcome, DurationMs, SourceIp, Context)
VALUES
    (@Origin, @UserId, @UserEmail, @EmployeeId, @Package, @IdentificationMasked, @Outcome, @DurationMs, @SourceIp, @Context);";

            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                record.Origin,
                record.UserId,
                UserEmail = Truncate(record.UserEmail, 256),
                record.EmployeeId,
                record.Package,
                IdentificationMasked = IdentificationMasker.Mask(record.Identification),
                record.Outcome,
                record.DurationMs,
                SourceIp = Truncate(record.SourceIp, 64),
                Context = Truncate(record.Context, 200),
            }, cancellationToken: ct));
        }
        catch (Exception ex)
        {
            // La auditoría nunca debe impedir la consulta del usuario.
            _logger.LogWarning(ex, "No se pudo registrar la consulta a DINARDAP ({Package}).", record.Package);
        }
    }

    /// <inheritdoc/>
    public async Task<PagedResult<DinardapQueryLogItemDto>> SearchAsync(
        DinardapQueryLogFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var where = new List<string>();
        var parameters = new DynamicParameters();

        if (filter.From.HasValue)
        {
            where.Add("l.QueriedAt >= @From");
            parameters.Add("From", filter.From.Value.ToDateTime(TimeOnly.MinValue));
        }
        if (filter.To.HasValue)
        {
            where.Add("l.QueriedAt < @ToExclusive");
            parameters.Add("ToExclusive", filter.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        }
        if (!string.IsNullOrWhiteSpace(filter.User))
        {
            where.Add("(l.UserEmail LIKE @User OR (p.LastName + N' ' + p.FirstName) LIKE @User)");
            parameters.Add("User", $"%{filter.User.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(filter.Package)) { where.Add("l.Package = @Package"); parameters.Add("Package", filter.Package.Trim().ToUpperInvariant()); }
        if (!string.IsNullOrWhiteSpace(filter.Origin)) { where.Add("l.Origin = @Origin"); parameters.Add("Origin", filter.Origin.Trim().ToUpperInvariant()); }
        if (!string.IsNullOrWhiteSpace(filter.Outcome)) { where.Add("l.Outcome = @Outcome"); parameters.Add("Outcome", filter.Outcome.Trim().ToUpperInvariant()); }

        var whereSql = where.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", where);
        const string from = @"
FROM HR.tbl_DinardapQueryLog l
LEFT JOIN HR.tbl_Employees e ON e.EmployeeID = l.EmployeeId
LEFT JOIN HR.tbl_People p ON p.PersonID = e.PersonID";

        parameters.Add("Skip", (page - 1) * pageSize);
        parameters.Add("Take", pageSize);

        using var connection = new SqlConnection(_connectionString);
        var total = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition($"SELECT COUNT_BIG(*) {from} {whereSql}", parameters, cancellationToken: ct));

        var rows = await connection.QueryAsync<DinardapQueryLogItemDto>(new CommandDefinition($@"
SELECT l.LogID AS Id, l.QueriedAt, l.Origin, l.UserId, l.UserEmail,
       LTRIM(RTRIM(p.LastName + N' ' + p.FirstName)) AS UserFullName,
       l.EmployeeId, l.Package, l.IdentificationMasked, l.Outcome, l.DurationMs, l.SourceIp, l.Context
{from}
{whereSql}
ORDER BY l.QueriedAt DESC, l.LogID DESC
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY", parameters, cancellationToken: ct));

        return new PagedResult<DinardapQueryLogItemDto>
        {
            Items = rows.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
        };
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value[..maxLength];
}

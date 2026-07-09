using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;

namespace WsUtaSystem.Infrastructure.Repositories;

public class ContractTypeRepository : ServiceAwareEfRepository<ContractType, int>, IContractTypeRepository
{
    public ContractTypeRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }

    public async Task<ContractType?> GetWithDefaultTemplateAsync(int contractTypeId, CancellationToken ct = default)
        => await _db.Set<ContractType>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContractTypeId == contractTypeId, ct);

    public async Task SetDefaultTemplateAsync(int contractTypeId, int? templateId, CancellationToken ct = default)
    {
        var entity = await _db.Set<ContractType>()
            .FirstOrDefaultAsync(x => x.ContractTypeId == contractTypeId, ct)
            ?? throw new KeyNotFoundException($"ContractType id={contractTypeId} no existe.");

        entity.DefaultTemplateId = templateId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetDelegationTemplateAsync(int contractTypeId, int? templateId, CancellationToken ct = default)
    {
        var entity = await _db.Set<ContractType>()
            .FirstOrDefaultAsync(x => x.ContractTypeId == contractTypeId, ct)
            ?? throw new KeyNotFoundException($"ContractType id={contractTypeId} no existe.");

        entity.DelegationTemplateId = templateId;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(string DocumentNumber, int Year, int Sequence)> ConsumeNextNumberAsync(
        int contractTypeId,
        int year,
        CancellationToken ct = default)
    {
        // UPDATE atómico con OUTPUT: evita el bug de entity tracker + HasDefaultValue
        // + MARS que causaba duplicados en la secuencia de contratos.
        const string sql = """
            UPDATE HR.tbl_contract_type
            SET   NumberingLastSequence = CASE
                      WHEN NumberingYear = @year THEN NumberingLastSequence + 1
                      ELSE 1
                  END,
                  NumberingYear = @year,
                  UpdatedAt     = GETDATE()
            OUTPUT INSERTED.NumberingPrefix, INSERTED.ContractCode, INSERTED.NumberingLastSequence
            WHERE  ContractTypeID = @id
            """;

        var conn = (SqlConnection)_db.Database.GetDbConnection();
        bool opened = conn.State != System.Data.ConnectionState.Open;
        if (opened) await conn.OpenAsync(ct);

        try
        {
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", contractTypeId);
            cmd.Parameters.AddWithValue("@year", year);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                throw new KeyNotFoundException($"ContractType id={contractTypeId} no existe.");

            var rawPrefix   = reader.IsDBNull(0) ? null : reader.GetString(0);
            var contractCode = reader.IsDBNull(1) ? null : reader.GetString(1);
            var sequence     = reader.GetInt32(2);

            var prefix = string.IsNullOrWhiteSpace(rawPrefix)
                ? (contractCode?.Trim() ?? $"CT{contractTypeId}")
                : rawPrefix.Trim();

            return ($"{prefix}-{year}-{sequence:D3}", year, sequence);
        }
        finally
        {
            if (opened) conn.Close();
        }
    }
}

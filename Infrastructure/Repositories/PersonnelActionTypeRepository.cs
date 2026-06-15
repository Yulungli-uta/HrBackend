using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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

    public async Task<(string DocumentNumber, int Year, int Sequence)> ConsumeNextNumberAsync(
        int personnelActionTypeId,
        int year,
        CancellationToken ct = default)
    {
        // UPDATE atómico con OUTPUT: evita el bug de entity tracker + HasDefaultValue
        // + MARS que causaba duplicados en UQ_PersonnelActions_ActionNumber.
        // No requiere transacción explícita: el UPDATE de una sola fila es atómico en SQL Server.
        const string sql = """
            UPDATE HR.tbl_Personnel_Action_Type
            SET   NumberingLastSequence = CASE
                      WHEN NumberingYear = @year THEN NumberingLastSequence + 1
                      ELSE 1
                  END,
                  NumberingYear = @year,
                  UpdatedAt     = GETDATE()
            OUTPUT INSERTED.NumberingPrefix, INSERTED.NumberingLastSequence
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
            return ($"{prefix}-{year}-{sequence:D3}", year, sequence);
        }
        finally
        {
            if (opened) conn.Close();
        }
    }
}

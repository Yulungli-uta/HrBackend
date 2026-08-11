using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>Único consumidor: <see cref="WsUtaSystem.Application.Services.TimeRecoveryLogsService"/> (también obsoleto). La lectura de HR.tbl_TimeRecoveryLogs sigue viva vía SQL puro (sp_ProcessAttendanceRecoveryDay), no vía este repositorio.</summary>
#pragma warning disable CS0618
[Obsolete("Solo el CRUD de escritura está sin uso real — ver TimeRecoveryLogsController.")]
public class TimeRecoveryLogsRepository : ServiceAwareEfRepository<TimeRecoveryLogs, int>, ITimeRecoveryLogsRepository
{
    public TimeRecoveryLogsRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
#pragma warning restore CS0618

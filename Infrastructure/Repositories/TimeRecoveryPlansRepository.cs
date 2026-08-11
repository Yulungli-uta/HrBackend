using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Infrastructure.Common;
using WsUtaSystem.Models;
namespace WsUtaSystem.Infrastructure.Repositories;

/// <summary>Único consumidor: <see cref="WsUtaSystem.Application.Services.TimeRecoveryPlansService"/> (también obsoleto). La lectura de HR.tbl_TimeRecoveryPlans sigue viva vía SQL puro (sp_ProcessAttendanceRecoveryDay), no vía este repositorio.</summary>
#pragma warning disable CS0618
[Obsolete("Solo el CRUD de escritura está sin uso real — ver TimeRecoveryPlansController.")]
public class TimeRecoveryPlansRepository : ServiceAwareEfRepository<TimeRecoveryPlans, int>, ITimeRecoveryPlansRepository
{
    public TimeRecoveryPlansRepository(WsUtaSystem.Data.AppDbContext db) : base(db) { }
}
#pragma warning restore CS0618

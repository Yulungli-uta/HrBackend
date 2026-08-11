using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;

/// <summary>Solo el CRUD de escritura está sin uso real — la lectura de la tabla sigue viva (ver <see cref="WsUtaSystem.Controllers.HR.TimeRecoveryPlansController"/>).</summary>
#pragma warning disable CS0618
[Obsolete("Solo el CRUD de escritura está sin uso real — el mecanismo vigente para PLANIFICAR es HR.tbl_TimePlanning (PlanType='Recovery'). La LECTURA de esta tabla sigue viva (sp_ProcessAttendanceRecoveryDay).")]
public class TimeRecoveryPlansService : Service<TimeRecoveryPlans, int>, ITimeRecoveryPlansService
{
    public TimeRecoveryPlansService(ITimeRecoveryPlansRepository repo) : base(repo) { }
}
#pragma warning restore CS0618

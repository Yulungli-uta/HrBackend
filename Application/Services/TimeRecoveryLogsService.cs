using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;

/// <summary>Solo el CRUD de escritura está sin uso real — la lectura de la tabla sigue viva (ver <see cref="WsUtaSystem.Controllers.HR.TimeRecoveryLogsController"/>).</summary>
#pragma warning disable CS0618
[Obsolete("Solo el CRUD de escritura está sin uso real — la EJECUCIÓN de recuperación planificada se registra en HR.tbl_TimePlanningExecution. La LECTURA de esta tabla sigue viva (sp_ProcessAttendanceRecoveryDay).")]
public class TimeRecoveryLogsService : Service<TimeRecoveryLogs, int>, ITimeRecoveryLogsService
{
    public TimeRecoveryLogsService(ITimeRecoveryLogsRepository repo) : base(repo) { }
}
#pragma warning restore CS0618

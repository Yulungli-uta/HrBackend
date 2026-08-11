using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Solo el CRUD de escritura está sin uso real — la lectura de la tabla sigue viva (ver <see cref="WsUtaSystem.Controllers.HR.TimeRecoveryLogsController"/>).</summary>
[Obsolete("Solo el CRUD de escritura está sin uso real — la EJECUCIÓN de recuperación planificada se registra en HR.tbl_TimePlanningExecution. La LECTURA de esta tabla sigue viva (sp_ProcessAttendanceRecoveryDay).")]
public interface ITimeRecoveryLogsService : IService<TimeRecoveryLogs, int> { }

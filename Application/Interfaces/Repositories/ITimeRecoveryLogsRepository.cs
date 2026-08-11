using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Repositories;

/// <summary>Solo el CRUD de escritura está sin uso real — la lectura de HR.tbl_TimeRecoveryLogs sigue viva vía SQL puro (sp_ProcessAttendanceRecoveryDay), no vía este repositorio.</summary>
[Obsolete("Solo el CRUD de escritura está sin uso real — ver TimeRecoveryLogsController.")]
public interface ITimeRecoveryLogsRepository : IRepository<TimeRecoveryLogs, int> { }

using WsUtaSystem.Models;
using WsUtaSystem.Application.Common.Interfaces;
namespace WsUtaSystem.Application.Interfaces.Services;

/// <summary>Solo el CRUD de escritura está sin uso real — la lectura de la tabla sigue viva (ver <see cref="WsUtaSystem.Controllers.HR.TimeRecoveryPlansController"/>).</summary>
[Obsolete("Solo el CRUD de escritura está sin uso real — el mecanismo vigente para PLANIFICAR es HR.tbl_TimePlanning (PlanType='Recovery'). La LECTURA de esta tabla sigue viva (sp_ProcessAttendanceRecoveryDay).")]
public interface ITimeRecoveryPlansService : IService<TimeRecoveryPlans, int> { }

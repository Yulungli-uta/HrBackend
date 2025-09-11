using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class AttendanceCalculationsService : Service<AttendanceCalculations, int>, IAttendanceCalculationsService
{
    public AttendanceCalculationsService(IAttendanceCalculationsRepository repo) : base(repo) { }
}

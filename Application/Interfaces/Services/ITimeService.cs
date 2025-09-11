using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Interfaces.Services
{
    public interface ITimeService
    {
        TimeResponse GetCurrentTime();
        TimeResponse GetCurrentTimeUtc();
        TimeResponse GetTimeByTimeZone(string timeZoneId);
    }
}

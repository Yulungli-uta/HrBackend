using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class TimeService : ITimeService
    {
        private readonly ILogger<TimeService> _logger;
        private readonly string _serverName;
        private readonly IAppClock _clock;

        public TimeService(ILogger<TimeService> logger, IConfiguration configuration, IAppClock clock)
        {
            _logger = logger;
            _clock = clock;
            _serverName = configuration["ServerName"] ?? Environment.MachineName;
        }

        public TimeResponse GetCurrentTime()
        {
            _logger.LogDebug("Getting current server time");

            var now = _clock.Now;
            return new TimeResponse
            {
                DateTime = now,
                Timestamp = _clock.NowOffset.ToUnixTimeSeconds(),
                TimeZone = _clock.Zone.Id,
                FormattedTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
                IsUtc = false,
                ServerName = _serverName
            };
        }

        public TimeResponse GetCurrentTimeUtc()
        {
            _logger.LogDebug("Getting current UTC time");

            var utcNow = _clock.UtcNow;
            return new TimeResponse
            {
                DateTime = utcNow.UtcDateTime,
                Timestamp = utcNow.ToUnixTimeSeconds(),
                TimeZone = "UTC",
                FormattedTime = utcNow.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                IsUtc = true,
                ServerName = _serverName
            };
        }

        public TimeResponse GetTimeByTimeZone(string timeZoneId)
        {
            _logger.LogDebug("Getting time for timezone: {TimeZoneId}", timeZoneId);

            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var zonedTime = TimeZoneInfo.ConvertTime(_clock.UtcNow, timeZone);

                return new TimeResponse
                {
                    DateTime = zonedTime.DateTime,
                    Timestamp = zonedTime.ToUnixTimeSeconds(),
                    TimeZone = timeZoneId,
                    FormattedTime = zonedTime.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    IsUtc = false,
                    ServerName = _serverName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time for timezone: {TimeZoneId}", timeZoneId);
                throw;
            }
        }
    }
}

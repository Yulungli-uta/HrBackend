using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class TimeService : ITimeService
    {
        private readonly ILogger<TimeService> _logger;
        private readonly string _serverName;

        public TimeService(ILogger<TimeService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _serverName = configuration["ServerName"] ?? Environment.MachineName;
        }

        public TimeResponse GetCurrentTime()
        {
            _logger.LogDebug("Getting current server time");

            var now = DateTime.Now;
            return new TimeResponse
            {
                DateTime = now,
                Timestamp = ((DateTimeOffset)now).ToUnixTimeSeconds(),
                TimeZone = TimeZoneInfo.Local.Id,
                FormattedTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
                IsUtc = false,
                ServerName = _serverName
            };
        }

        public TimeResponse GetCurrentTimeUtc()
        {
            _logger.LogDebug("Getting current UTC time");

            var now = DateTime.UtcNow;
            return new TimeResponse
            {
                DateTime = now,
                Timestamp = ((DateTimeOffset)now).ToUnixTimeSeconds(),
                TimeZone = "UTC",
                FormattedTime = now.ToString("yyyy-MM-dd HH:mm:ss"),
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
                var utcNow = DateTime.UtcNow;
                var zonedTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);

                return new TimeResponse
                {
                    DateTime = zonedTime,
                    Timestamp = ((DateTimeOffset)zonedTime).ToUnixTimeSeconds(),
                    TimeZone = timeZoneId,
                    FormattedTime = zonedTime.ToString("yyyy-MM-dd HH:mm:ss"),
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

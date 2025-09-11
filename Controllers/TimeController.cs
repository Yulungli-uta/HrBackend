using Microsoft.AspNetCore.Mvc;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Controllers
{
    [ApiController]
    [Route("time")]
    public class TimeController : ControllerBase
    {
        private readonly ITimeService _timeService;
        private readonly ILogger<TimeController> _logger;

        public TimeController(ITimeService timeService, ILogger<TimeController> logger)
        {
            _timeService = timeService;
            _logger = logger;
        }

        /// <summary>
        /// Gets current server time in local timezone
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(TimeResponse), 200)]
        public ActionResult<TimeResponse> GetServerTime()
        {
            _logger.LogInformation("Time service called - local time");
            var response = _timeService.GetCurrentTime();
            return Ok(response);
        }

        /// <summary>
        /// Gets current server time in UTC
        /// </summary>
        [HttpGet("utc")]
        [ProducesResponseType(typeof(TimeResponse), 200)]
        public ActionResult<TimeResponse> GetServerTimeUtc()
        {
            _logger.LogInformation("Time service called - UTC time");
            var response = _timeService.GetCurrentTimeUtc();
            return Ok(response);
        }

        /// <summary>
        /// Gets current time for specified timezone
        /// </summary>
        /// <param name="timeZoneId">Timezone ID (e.g., "America/New_York")</param>
        [HttpGet("timezone/{timeZoneId}")]
        [ProducesResponseType(typeof(TimeResponse), 200)]
        [ProducesResponseType(400)]
        public ActionResult<TimeResponse> GetTimeByTimeZone(string timeZoneId)
        {
            _logger.LogInformation("Time service called - timezone: {TimeZoneId}", timeZoneId);

            try
            {
                var response = _timeService.GetTimeByTimeZone(timeZoneId);
                return Ok(response);
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogWarning(ex, "Invalid timezone requested: {TimeZoneId}", timeZoneId);
                return BadRequest($"Invalid timezone: {timeZoneId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing timezone request: {TimeZoneId}", timeZoneId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(200)]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "TimeService"
            });
        }
    }
}

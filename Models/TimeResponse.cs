namespace WsUtaSystem.Models
{
    public class TimeResponse
    {
        public DateTime DateTime { get; set; }
        public long Timestamp { get; set; }
        public string TimeZone { get; set; }
        public string FormattedTime { get; set; }
        public bool IsUtc { get; set; }
        public string ServerName { get; set; }
    }
}

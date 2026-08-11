namespace WsUtaSystem.Application.DTOs.Reports
{
    public sealed class ScheduleCoverageStatsDto
    {
        public int Total { get; set; }
        public int WithSchedule { get; set; }
        public int WithoutSchedule { get; set; }
    }
}

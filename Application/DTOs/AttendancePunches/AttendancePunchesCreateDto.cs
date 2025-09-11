namespace WsUtaSystem.Application.DTOs.AttendancePunches;
public class AttendancePunchesCreateDto
{
    //public class AttendancePunches { get; set; }
    public int PunchId { get; set; }
    public int EmployeeId { get; set; }
    public DateTime PunchTime { get; set; }
    public string PunchType { get; set; }
    public string DeviceId { get; set; }
    public decimal Longitude { get; set; }
    public decimal Latitude { get; set; }
    public DateTime CreatedAt { get; set; }
}

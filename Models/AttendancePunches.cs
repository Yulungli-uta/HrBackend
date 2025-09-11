
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WsUtaSystem.Models;
public class AttendancePunches {

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PunchId { get; set; }
  public int EmployeeId { get; set; }
  public DateTime? PunchTime { get; set; }
  public string PunchType { get; set; } = null!;
  public string? DeviceId { get; set; }
  public decimal? Longitude { get; set; }
  public decimal? Latitude { get; set; }
  public DateTime CreatedAt { get; set; }
}

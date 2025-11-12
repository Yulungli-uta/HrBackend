
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace WsUtaSystem.Models;
//[Table("tbl_PunchJustifications", Schema = "HR")]
public class PunchJustifications
{
    [Column("PunchJustID")]
    public int PunchJustId { get; set; }    
    public int EmployeeId { get; set; }
    public int BossEmployeeId { get; set; }
    public int JustificationTypeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? JustificationDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? HoursRequested { get; set; }
    public bool Approved { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string? Comments { get; set; }
    public string Status { get; set; } = "PENDING";
}

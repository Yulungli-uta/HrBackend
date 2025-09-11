
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using Microsoft.EntityFrameworkCore;

namespace WsUtaSystem.Models;
//[Table("tbl_PunchJustifications", Schema = "HR")]
public class PunchJustifications
{
    //[Key]
    //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PunchJustId { get; set; }

    //public int? PunchID { get; set; }

    //[Required]
    public int EmployeeId { get; set; }

    //[Required]
    public int BossEmployeeId { get; set; }

    //[Required]
    public int JustificationTypeId { get; set; }

    public DateTime? StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    public DateOnly? JustificationDate { get; set; }

    //[Required]
    //[MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    //[Precision(4, 2)]
    public decimal? HoursRequested { get; set; }

    //[Required]
    public bool Approved { get; set; } = false;

    public DateTime? ApprovedAt { get; set; }

    //[Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    //[Required]
    public int CreatedBy { get; set; }

    //[MaxLength(1000)]
    public string? Comments { get; set; }

    //[Required]
    //[MaxLength(20)]
    public string Status { get; set; } = "PENDING";
}

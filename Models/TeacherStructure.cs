using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models;

public class TeacherStructure : IAuditable
{
    public int TeacherStructureId { get; set; }
    public int EmployeeId { get; set; }
    public int? LadderId { get; set; }
    public int DedicationTypeId { get; set; }
    public decimal? WeeklyClassHours { get; set; }
    public decimal? HourValue { get; set; }
    public decimal? Rmu { get; set; }
    public int? DepartmentId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EligiblePromotion { get; set; }
    public bool EligibleRecategory { get; set; }
    public bool EligibleDedicChg { get; set; }
    public DateTime? CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    // Navegación
    public virtual Employees? Employee { get; set; }
    public virtual AcademicLadder? Ladder { get; set; }
    public virtual RefTypes? DedicationType { get; set; }
    public virtual Departments? Department { get; set; }
}

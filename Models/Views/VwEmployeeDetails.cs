using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WsUtaSystem.Models.Views
{
    public class VwEmployeeDetails
    {
 
        public int EmployeeID { get; set; }     
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;     
        public string IDCard { get; set; } = string.Empty;   
        /// <summary>Email institucional. Puede ser null en empleados recién creados sin cuenta asignada aún.</summary>
        public string? Email { get; set; }
        /// <summary>Email personal. Puede ser null si no fue registrado en tbl_People.</summary>
        public string? PersonnelEmail { get; set; }
        public int EmployeeType { get; set; }
        [Column("ContractType")]
        public string? ContractType { get; set; }
        [Column("ScheduleID")]
        public int? ScheduleID { get; set; }
        [Column("Schedule")]
        public string? Schedule { get; set; }
        [Column("ImmediateBossID")]
        public int? ImmediateBossID { get; set; }
        public int? DepartmentID { get; set; }
        public string? Department { get; set; }
        public int? JobId { get; set; }
        public string? JobName { get; set; }
        //public string? Faculty { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BaseSalary { get; set; }     
        public DateTime HireDate { get; set; }
        // Propiedades calculadas
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
        [NotMapped]
        public bool HasActiveSalary => BaseSalary.HasValue && BaseSalary > 0;

    }
}

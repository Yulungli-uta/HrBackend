using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WsUtaSystem.Models
{
    public class VwEmployeeDetails
    {
 
        public int EmployeeID { get; set; }     
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;     
        public string IDCard { get; set; } = string.Empty;   
        public string Email { get; set; } = string.Empty;
        public int EmployeeType { get; set; }
        [Column("ContractType")]
        public string? ContractType { get; set; }
        [Column("ScheduleID")]
        public int? ScheduleID { get; set; }
        [Column("Schedule")]
        public string? Schedule { get; set; }
        public int? ImmediateBossID { get; set; }
        public string? Department { get; set; }
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

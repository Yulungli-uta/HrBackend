using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WsUtaSystem.Models
{
    public class Job
    {
        //[Key]
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int JobID { get; set; }

        //[Required]
        //[MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        //[Column(TypeName = "TEXT")]
        public string? Description { get; set; }

     
        public bool IsActive { get; set; } = true;

   
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}

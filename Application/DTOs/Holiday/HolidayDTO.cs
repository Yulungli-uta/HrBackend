using System.ComponentModel.DataAnnotations;

namespace WsUtaSystem.Application.DTOs.Holiday
{
    public class HolidayCreateDTO
    {
        [Required(ErrorMessage = "El nombre del feriado es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha del feriado es requerida")]
        public DateTime HolidayDate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres")]
        public string? Description { get; set; }
    }

    public class HolidayUpdateDTO
    {
        [Required(ErrorMessage = "El ID del feriado es requerido")]
        public int HolidayID { get; set; }

        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        public string? Name { get; set; }

        public DateTime? HolidayDate { get; set; }
        public bool? IsActive { get; set; }

        [StringLength(255, ErrorMessage = "La descripción no puede exceder 255 caracteres")]
        public string? Description { get; set; }
    }

    public class HolidayResponseDTO
    {
        public int HolidayID { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime HolidayDate { get; set; }
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

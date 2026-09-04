namespace WsUtaSystem.Application.DTOs.Jobs
{
    public class JobDto
    {
        public int JobID { get; set; }
        public string? Description { get; set; }
        public int? JobTypeId { get; set; }
        public int? GroupId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? SiiesTipoFuncionarioTypeId { get; set; }
        public bool PuestoJerarquicoSuperior { get; set; }
        public decimal? ReferenceSalary { get; set; }
        /// <summary>FK -> tbl_AcademicLadder. Enlace estructural para Jobs docentes (Profesor Titular Auxiliar/Agregado/Principal) hacia su escalón real de escalafón. Nulo para Jobs administrativos.</summary>
        public int? AcademicLadderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}

namespace WsUtaSystem.Application.DTOs.Jobs
{
    public class UpdateJobDto
    {
        public int JobID { get; set; }
        public string? Description { get; set; }
        public int? JobTypeId { get; set; }
        public int? GroupId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? SiiesTipoFuncionarioTypeId { get; set; }
        public bool PuestoJerarquicoSuperior { get; set; }
        public decimal? ReferenceSalary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

namespace WsUtaSystem.Application.DTOs.Parameters
{
    public class ParametersDto
    {
        public int ParameterId { get; set; }
        public string Name { get; set; } = null!;
        public string? Pvalues { get; set; }
        public string? Description { get; set; }
        public string? DataType { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}


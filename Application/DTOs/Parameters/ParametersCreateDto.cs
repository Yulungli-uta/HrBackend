namespace WsUtaSystem.Application.DTOs.Parameters
{
    public class ParametersCreateDto
    {
        public string Name { get; set; } = null!;
        public string? Pvalues { get; set; }
        public string? Description { get; set; }
        public string? DataType { get; set; }
        public bool IsActive { get; set; } = true;
        public int? CreatedBy { get; set; }
    }
}


namespace WsUtaSystem.Application.DTOs.Parameters
{
    public class ParametersUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Pvalues { get; set; }
        public string? Description { get; set; }
        public string? DataType { get; set; }
        public bool IsActive { get; set; }
        public string? UpdatedBy { get; set; }
    }
}


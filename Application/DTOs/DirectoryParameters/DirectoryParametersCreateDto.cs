namespace WsUtaSystem.Application.DTOs.DirectoryParameters
{
    public class DirectoryParametersCreateDto
    {
        public string Code { get; set; } = null!;
        public string PhysicalPath { get; set; } = null!;
        public string? RelativePath { get; set; }
        public string? Description { get; set; }
        public string? Extension { get; set; }
        public int? MaxSizeMb { get; set; }
        public bool Status { get; set; } = true;
        public int? CreatedBy { get; set; }
    }
}


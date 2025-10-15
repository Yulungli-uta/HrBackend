namespace WsUtaSystem.Application.DTOs.DirectoryParameters
{
    public class DirectoryParametersDto
    {
        public int DirectoryId { get; set; }
        public string Code { get; set; } = null!;
        public string PhysicalPath { get; set; } = null!;
        public string? RelativePath { get; set; }
        public string? Description { get; set; }
        public string? Extension { get; set; }
        public int? MaxSizeMb { get; set; }
        public bool Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}


namespace WsUtaSystem.Application.DTOs.Docflow
{
    public sealed class DocumentDto
    {
        public Guid DocumentId { get; set; }
        public Guid InstanceId { get; set; }
        public int? RuleId { get; set; }
        public string DocumentName { get; set; } = null!;
        public int CreatedByDepartmentId { get; set; }
        public byte Visibility { get; set; }
        public int CurrentVersion { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    
}

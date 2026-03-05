namespace WsUtaSystem.Application.DTOs.Docflow
{
    
    public sealed class InstanceListItemDto
    {
        public Guid InstanceId { get; set; }
        public int ProcessId { get; set; }
        public int? RootProcessId { get; set; }
        public string? InstanceName { get; set; }
        public string CurrentStatus { get; set; } = null!;
        public int CurrentDepartmentId { get; set; }
        public int? AssignedToUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class InstanceDetailDto
    {
        public Guid InstanceId { get; set; }
        public int ProcessId { get; set; }
        public int? RootProcessId { get; set; }
        public string? InstanceName { get; set; }
        public string CurrentStatus { get; set; } = null!;
        public int CurrentDepartmentId { get; set; }
        public int? AssignedToUserId { get; set; }
        public string? DynamicMetadata { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    
}

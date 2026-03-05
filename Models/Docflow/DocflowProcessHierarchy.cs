using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Docflow
{
    public class DocflowProcessHierarchy : IAuditable
    {
        public int ProcessId { get; set; }
        public int? ParentId { get; set; }
        public string ProcessCode { get; set; } = null!;
        public string ProcessName { get; set; } = null!;
        public int ResponsibleDepartmentId { get; set; }
        public string ProcessFolderName { get; set; }
        public string DynamicFieldMetadata { get; set; }        
        public bool IsActive { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

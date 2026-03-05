using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Docflow
{
    public class DocflowDocument : IAuditable
    {
        public Guid DocumentId { get; set; }
        public Guid InstanceId { get; set; }
        public int? RuleId { get; set; }
        public string DocumentName { get; set; } = null!;

        public int CreatedByDepartmentId { get; set; }

        /// <summary>1=PUBLIC_WITHIN_CASE, 2=PRIVATE_TO_UPLOADER_DEPT</summary>
        public byte Visibility { get; set; }

        public int CurrentVersion { get; set; }
        public bool IsDeleted { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

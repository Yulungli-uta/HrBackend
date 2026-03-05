using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Docflow
{
    public class DocflowDocumentRule : IAuditable
    {
        public int RuleId { get; set; }
        public int ProcessId { get; set; }
        public string DocumentType { get; set; } = null!;
        public bool IsRequired { get; set; }

        /// <summary>1=PUBLIC_WITHIN_CASE, 2=PRIVATE_TO_UPLOADER_DEPT</summary>
        public byte DefaultVisibility { get; set; }

        public bool AllowVisibilityOverride { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }    
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

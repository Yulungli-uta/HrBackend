using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class EmailLogAttachment : ICreationAuditable
    {
        public int EmailLogAttachmentID { get; set; }

        public int EmailLogID { get; set; }
        public Guid StoredFileGuid { get; set; }

        public string? FileName { get; set; }
        public string? ContentType { get; set; }

        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public EmailLog? EmailLog { get; set; }

        // Optional navigation if you want it (recommended)
        public StoredFile? StoredFile { get; set; }
    }
}

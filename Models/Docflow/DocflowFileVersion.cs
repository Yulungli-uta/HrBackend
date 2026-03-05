using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models.Docflow
{
    public class DocflowFileVersion : ICreationAuditable
    {
        public Guid VersionId { get; set; }
        public Guid DocumentId { get; set; }
        public int VersionNumber { get; set; }

        public string StoragePath { get; set; } = null!;
        public string? FileExtension { get; set; }
        public long? FileSizeInBytes { get; set; }
        public string? ChecksumHash { get; set; }

        public int? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}

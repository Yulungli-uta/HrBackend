using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class StoredFile : IAuditable
    {
        public int FileId { get; set; }

        // Public identifier (API-safe)
        public Guid FileGuid { get; set; }

        // FK -> HR.TBL_DirectoryParameters(Code)
        public string DirectoryCode { get; set; } = null!;

        // Business context
        public string EntityType { get; set; } = null!;  // e.g., "CONTRACT"
        public string EntityId { get; set; } = null!;    // e.g., "987"

        // Location parts (NO full path stored)
        public int UploadYear { get; set; }
        public string RelativeFolder { get; set; } = null!; // e.g., "contracts\\2025\\987\\"
        public string StoredFileName { get; set; } = null!; // e.g., "anexo_1.pdf"
        public string? OriginalFileName { get; set; }
        public int? DocumentTypeId { get; set; }

        // Metadata
        public string? Extension { get; set; }     // ".pdf"
        public string? ContentType { get; set; }   // "application/pdf"
        public long SizeBytes { get; set; }
        public byte[]? Sha256 { get; set; }        // binary(32)

        // Status (1=Active, 2=Deleted, 3=Archived)
        public byte Status { get; set; }

        // Audit
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Computed column in DB: FilePathHash (binary(32))
        // Puedes mapearla si quieres leerla, o ignorarla.
        public byte[]? FilePathHash { get; set; }
    }
}

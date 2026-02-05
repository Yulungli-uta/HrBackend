namespace WsUtaSystem.Application.DTOs.Email
{
    // ============================================================
    // EMAIL LAYOUTS (HR.tbl_EmailLayouts)
    // ============================================================

    /// <summary>
    /// Response DTO - Layout de correo
    /// </summary>
    public class EmailLayoutDto
    {
        public int EmailLayoutID { get; set; }
        public string Slug { get; set; } = string.Empty;

        public string? HeaderHtml { get; set; }
        public string? FooterHtml { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Create DTO - Layout de correo
    /// </summary>
    public class EmailLayoutCreateDto
    {
        public string Slug { get; set; } = string.Empty;

        public string? HeaderHtml { get; set; }
        public string? FooterHtml { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }
    }

    /// <summary>
    /// Update DTO - Layout de correo
    /// </summary>
    public class EmailLayoutUpdateDto
    {
        public string? HeaderHtml { get; set; }
        public string? FooterHtml { get; set; }

        public bool? IsActive { get; set; }

        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // ============================================================
    // EMAIL LOGS (HR.tbl_EmailLogs)
    // SOLO INSERCIÓN + LECTURA (NO UPDATE)
    // ============================================================

    /// <summary>
    /// Create DTO - Log de envío de correo
    /// (uso interno del servicio)
    /// </summary>
    public class EmailLogCreateDto
    {
        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;

        public string BodyRendered { get; set; } = string.Empty;

        /// <summary>
        /// Queued | Sent | Failed
        /// </summary>
        public string Status { get; set; } = "Queued";

        public DateTime SentAt { get; set; } = DateTime.Now;

        public string? ErrorMessage { get; set; }

        public int? CreatedBy { get; set; }
    }

    /// <summary>
    /// Response DTO - Log de correo
    /// </summary>
    public class EmailLogDto
    {
        public int EmailLogID { get; set; }

        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public List<EmailLogAttachmentDto> Attachments { get; set; } = new();
    }

    // ============================================================
    // EMAIL LOG ATTACHMENTS (HR.tbl_EmailLogAttachments)
    // SOLO INSERCIÓN + LECTURA (NO UPDATE)
    // ============================================================

    /// <summary>
    /// Create DTO - Adjuntos asociados a un EmailLog
    /// </summary>
    public class EmailLogAttachmentCreateDto
    {
        public int EmailLogID { get; set; }
        public Guid StoredFileGuid { get; set; }

        public string? FileName { get; set; }
        public string? ContentType { get; set; }

        public int? CreatedBy { get; set; }
    }

    /// <summary>
    /// Response DTO - Adjuntos del log de correo
    /// </summary>
    public class EmailLogAttachmentDto
    {
        public int EmailLogAttachmentID { get; set; }

        public Guid StoredFileGuid { get; set; }

        public string? FileName { get; set; }
        public string? ContentType { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
    }

    // ============================================================
    // EMAIL SEND (Request/Response)
    // ============================================================

    /// <summary>
    /// Request DTO - Enviar correo (soporta adjuntos híbridos)
    /// MEJORADO: Los IFormFile se pre-procesan antes de encolar para evitar problemas de stream cerrado.
    /// </summary>
    public class EmailSendRequestDto
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// HTML del cuerpo (sin header/footer; se aplicará layout si se indica).
        /// </summary>
        public string BodyHtml { get; set; } = string.Empty;

        /// <summary>
        /// Slug del layout (HR.tbl_EmailLayouts.Slug).
        /// Si es null/empty, se envía solo BodyHtml.
        /// </summary>
        public string? LayoutSlug { get; set; }

        /// <summary>
        /// Id de usuario/empleado para auditoría (CreatedBy).
        /// Si no lo envías, se guarda null.
        /// </summary>
        public int? CreatedBy { get; set; }

        /// <summary>
        /// Adjuntos híbridos: lista de EmailSendAttachmentDto.
        /// </summary>
        public List<EmailSendAttachmentDto> Attachments { get; set; } = new();
    }

    /// <summary>
    /// MEJORADO: Soporte para archivos pre-procesados (bytes en memoria).
    /// </summary>
    public class EmailSendAttachmentDto
    {
        /// <summary>
        /// Caso A: adjunto existente (FileGuid de HR.TBL_StoredFile).
        /// </summary>
        public Guid? StoredFileGuid { get; set; }

        /// <summary>
        /// Caso B: archivo nuevo (multipart/form-data).
        /// IMPORTANTE: Solo usar en contexto HTTP sincrónico.
        /// Para encolar, usar PreProcessedFile en su lugar.
        /// </summary>
        public IFormFile? File { get; set; }

        /// <summary>
        /// Caso B2: archivo pre-procesado (bytes en memoria).
        /// Se usa cuando se encola el email para evitar stream cerrado.
        /// </summary>
        public PreProcessedFileDto? PreProcessedFile { get; set; }

        // Metadata necesaria para registrar el archivo nuevo (si File o PreProcessedFile != null).
        public string? DirectoryCode { get; set; }
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public int? DocumentTypeId { get; set; }
        public string? RelativePath { get; set; }
    }

    /// <summary>
    /// DTO para archivo pre-procesado en memoria (evita stream cerrado).
    /// </summary>
    public class PreProcessedFileDto
    {
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long FileSize { get; set; }
    }

    public class EmailSendResponseDto
    {
        public bool Success { get; set; }
        public int? EmailLogID { get; set; }
        public string? Message { get; set; }
    }
}

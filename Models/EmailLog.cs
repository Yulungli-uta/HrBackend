using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class EmailLog : ICreationAuditable
    {
        public int EmailLogID { get; set; }

        public string Recipient { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;

        public string BodyRendered { get; set; } = string.Empty;

        /// <summary>
        /// Allowed values: Queued | Sent | Failed
        /// </summary>
        public string Status { get; set; } = "Queued";

        public DateTime SentAt { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public ICollection<EmailLogAttachment> Attachments { get; set; } = new List<EmailLogAttachment>();
    }
}

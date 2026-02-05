using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class EmailLayout : IAuditable
    {
        public int EmailLayoutID { get; set; }

        public string Slug { get; set; } = string.Empty;

        public string? HeaderHtml { get; set; }
        public string? FooterHtml { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

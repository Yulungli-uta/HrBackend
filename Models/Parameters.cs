using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class Parameters : IAuditable
    {
        public int ParameterId { get; set; }
        public string Name { get; set; } = null!;
        public string? Pvalues { get; set; }
        public string? Description { get; set; }
        public string? DataType { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }  
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }

    }
}


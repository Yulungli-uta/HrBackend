using DocumentFormat.OpenXml.Wordprocessing;
using WsUtaSystem.Application.Interfaces.Auditable;

namespace WsUtaSystem.Models
{
    public class KnowledgeArea : IAuditable
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public int Levels { get; set; }

        /// <summary>Código exacto exigido por el catálogo SIIES (Anexo Clasificación Internacional Normalizada de la Educación), ej. "3-11A". Pendiente de mapeo manual completo — ver decisión institucional.</summary>
        public string? SiiesCode { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }
}

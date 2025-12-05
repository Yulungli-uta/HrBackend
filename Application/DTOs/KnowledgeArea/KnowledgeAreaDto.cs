namespace WsUtaSystem.Application.DTOs.KnowledgeArea
{
    public class KnowledgeAreaDto
    {
        public int? Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int? ParentId { get; set; }
        public int Levels { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

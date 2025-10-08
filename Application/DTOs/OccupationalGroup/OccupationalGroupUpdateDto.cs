namespace WsUtaSystem.Application.DTOs.OccupationalGroup
{
    public class OccupationalGroupUpdateDto
    {
        public int GroupId { get; set; }
        public string Description { get; set; } = null!;
        public decimal Rmu { get; set; }
        public int DegreeId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

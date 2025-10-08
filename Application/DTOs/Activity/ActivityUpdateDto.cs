namespace WsUtaSystem.Application.DTOs.Activity
{
    public class ActivityUpdateDto
    {
        public int ActivitiesId { get; set; }
        public string? Description { get; set; }
        public string ActivitiesType { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

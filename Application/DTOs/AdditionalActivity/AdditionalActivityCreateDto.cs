namespace WsUtaSystem.Application.DTOs.AdditionalActivity
{
    public class AdditionalActivityCreateDto
    {
        public int ActivitiesId { get; set; }
        public int ContractId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        //public DateTime? UpdatedAt { get; set; }
    }
}

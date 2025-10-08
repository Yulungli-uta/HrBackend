namespace WsUtaSystem.Application.DTOs.JobActivity
{
    public class JobActivityCreateDto
    {
        public int ActivitiesId { get; set; }
        public int TblJobs { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        //public DateTime? UpdatedAt { get; set; }
    }
}

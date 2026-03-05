namespace WsUtaSystem.Application.DTOs.ContractStatusHistory
{
    public class ContractStatusHistoryDto
    {
        public int HistoryID { get; set; }
        public int ContractID { get; set; }
        public int StatusTypeID { get; set; }
        public string? StatusName { get; set; }  // viene de RefType.Name
        public string? Comment { get; set; }
        public DateTime ChangedAt { get; set; }
        public int? ChangedBy { get; set; }
    }
}

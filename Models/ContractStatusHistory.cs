namespace WsUtaSystem.Models
{
    public class ContractStatusHistory
    {
        public int HistoryID { get; set; }

        public int ContractID { get; set; }
        public int StatusTypeID { get; set; }

        public string? Comment { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.Now;
        public int? ChangedBy { get; set; }
    }
}

namespace WsUtaSystem.Models
{
    public class ContractType
    {
        public int ContractTypeId { get; set; }
        public int? PersonalContractTypeId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? ContractText { get; set; }
        public string? ContractCode { get; set; }
    }
}

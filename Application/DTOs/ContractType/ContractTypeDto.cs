namespace WsUtaSystem.Application.DTOs.ContractType
{
    public class ContractTypeDto
    {
        public int ContractTypeId { get; set; }
        public int? PersonalContractTypeId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? ContractText { get; set; }
        public string? ContractCode { get; set; }
        public int? DocumentTemplateTypeId { get; set; }
        public int? DefaultTemplateId { get; set; }
        public int? DelegationTemplateId { get; set; }
        public string? NumberingPrefix { get; set; }
        public int NumberingYear { get; set; }
        public int NumberingLastSequence { get; set; }
        public bool RequiresAdUserCreation { get; set; }
        public bool RequiresAdUserDisable { get; set; }
        public bool RequiresAdGroupAssignment { get; set; }
    }
}

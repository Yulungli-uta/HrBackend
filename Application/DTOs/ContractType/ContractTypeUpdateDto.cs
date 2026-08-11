namespace WsUtaSystem.Application.DTOs.ContractType
{
    public class ContractTypeUpdateDto
    {
        public int ContractTypeId { get; set; }
        public int? PersonalContractTypeId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? ContractText { get; set; }
        public string? ContractCode { get; set; }
        public int? SiiesRelacionIesTypeId { get; set; }
        public int? DefaultTemplateId { get; set; }
        public int? DelegationTemplateId { get; set; }
        public string? NumberingPrefix { get; set; }
        public bool RequiresAdUserCreation { get; set; } = false;
        public bool RequiresAdUserDisable { get; set; } = false;
        public bool RequiresAdGroupAssignment { get; set; } = false;
    }
}

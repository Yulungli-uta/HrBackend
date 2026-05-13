namespace WsUtaSystem.Application.DTOs.ContractRequestPerson
{
    public class ContractRequestPersonDto
    {
        public int RequestPersonId { get; set; }
        public int RequestId { get; set; }
        public int? PersonId { get; set; }
        public string? PersonFullName { get; set; }
        public string? PersonIdentification { get; set; }
        public int JobId { get; set; }
        public string? JobName { get; set; }
        public int RequestPersonTypeId { get; set; }
        public string? RequestPersonTypeName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? WeeklyClassHours { get; set; }
        public decimal? HourValue { get; set; }
        public decimal? MonthsPeriod { get; set; }
        public decimal? Rmu { get; set; }
        public decimal? RmuPeriod { get; set; }
        public int? EntrySourceId { get; set; }
        public string? EntrySourceName { get; set; }
        public bool IsHired { get; set; }
        public int? ContractId { get; set; }
        public int? StatusId { get; set; }
        public string? StatusName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
    }

    public class CreateContractRequestPersonDto
    {
        public int? PersonId { get; set; }
        public int JobId { get; set; }
        public int RequestPersonTypeId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? WeeklyClassHours { get; set; }
        public decimal? HourValue { get; set; }
    }

    public class UpdateContractRequestPersonDto
    {
        public int? PersonId { get; set; }
        public int JobId { get; set; }
        public int RequestPersonTypeId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal? WeeklyClassHours { get; set; }
        public decimal? HourValue { get; set; }
    }

    public class ContractRequestSlotsDto
    {
        public int RequestId { get; set; }
        public int NumberOfPeopleToHire { get; set; }
        public int TotalHired { get; set; }
        public int SlotsAvailable { get; set; }
        public int PendingPeople { get; set; }
    }

    public class HireRequestPersonDto
    {
        public int ContractId { get; set; }
    }

    public class GenerateContractFromAvailablePersonDto
    {
        public int PersonId { get; set; }
        public int ContractId { get; set; }
        public int JobId { get; set; }
    }

    public class AvailablePersonDto
    {
        public int PersonId { get; set; }
        public string? FullName { get; set; }
        public string? Identification { get; set; }
    }
}

public sealed record RejectTemporaryDto(string? Reason);
public sealed record ResendCertificationDto();

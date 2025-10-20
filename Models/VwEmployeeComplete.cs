namespace WsUtaSystem.Models
{

    public class VwEmployeeComplete
    {
        public int EmployeeID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? IDCard { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime BirthDate { get; set; }
        public int? Sex { get; set; }
        public int? Gender { get; set; }
        public string? Address { get; set; }
        public bool PersonIsActive { get; set; }
        public int EmployeeType { get; set; }
        public string? EmployeeTypeName { get; set; }
        public DateTime HireDate { get; set; }
        public bool EmployeeIsActive { get; set; }
        public string? Department { get; set; }
        //public string? Faculty { get; set; }
        public string? ImmediateBoss { get; set; }
        public int YearsOfService { get; set; }
        public int? MaritalStatusTypeID { get; set; }
        public string? MaritalStatus { get; set; }
        public int? EthnicityTypeID { get; set; }
        public string? Ethnicity { get; set; }
        public int? BloodTypeTypeID { get; set; }
        public string? BloodType { get; set; }
        public decimal? DisabilityPercentage { get; set; }
        public string? CONADISCard { get; set; }
        public string? CountryName { get; set; }
        public string? ProvinceName { get; set; }
        public string? CantonName { get; set; }
    }
}
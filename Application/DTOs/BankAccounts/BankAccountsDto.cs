namespace WsUtaSystem.Application.DTOs.BankAccounts;
public class BankAccountsDto
{
    //public class BankAccounts { get; set; }
    public int AccountId { get; set; }
    public int PersonId { get; set; }
    public string FinancialInstitution { get; set; }
    public int AccountTypeId { get; set; }
    public string AccountNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

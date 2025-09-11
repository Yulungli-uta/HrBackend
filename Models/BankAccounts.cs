
namespace WsUtaSystem.Models;
public class BankAccounts {
  public int AccountId{get;set;}
  public int PersonId{get;set;}
  public string FinancialInstitution{get;set;}=null!;
  public int AccountTypeId{get;set;}
  public string AccountNumber{get;set;}=null!;
  public DateTime CreatedAt{get;set;}
}

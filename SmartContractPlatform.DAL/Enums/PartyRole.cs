namespace SmartContractPlatform.DAL.Enums;

public enum PartyRole
{
    Owner = 0,
    Tenant = 1,
    Buyer = 2,
    Seller = 3,
    Client = 4,
    Provider = 5,
    Borrower = 6,
    Lender = 7,
    ServiceProvider = Provider,
    Debtor = Borrower,
    Creditor = Lender
}



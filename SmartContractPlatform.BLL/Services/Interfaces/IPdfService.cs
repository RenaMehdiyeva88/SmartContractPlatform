using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.BLL.Services.Interfaces;

public interface IPdfService
{
    byte[] GenerateContractPdf(Contract contract, string uniqueHash);
}

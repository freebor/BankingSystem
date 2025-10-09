using BankingSystem.Models.Entity;

namespace BankingSystem.Services.Interface
{
    public interface IAccountService
    {
        Task<Account> CreateAccountAsync(Guid userId, string currency);
        Task<Account?> GetAccountByIdAsync(Guid accountId);
        Task<Account?> GetAccountByNumberAsync(string accountNumber);
        Task<IEnumerable<Account>> GetUserAccountsAsync(Guid userId);
        Task<decimal> GetBalanceAsync(Guid accountId);
    }
}

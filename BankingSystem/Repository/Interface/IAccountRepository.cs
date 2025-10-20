using BankingSystem.Models.Entity;

namespace BankingSystem.Repository.Interface
{
    public interface IAccountRepository
    {
        Task<Account> CreateAccountAsync(Account account);
        Task<Account?> GetByIdAsync(Guid id);
        Task<Account?> GetByAccountNumberAsync(string accountNumber);
        Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId);
        Task<bool> RemoveAccountAsync(Guid userId);
        Task<bool> CloseAccountAsync(Guid accountId);
        Task UpdateBalanceAsync(Guid accountId, decimal newBalance);
    }
}

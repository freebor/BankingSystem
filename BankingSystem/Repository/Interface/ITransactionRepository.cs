using BankingSystem.Models.Entity;

namespace BankingSystem.Repository.Interface
{
    public interface ITransactionRepository
    {
        Task<Transaction> CreateTransactionAsync(Transaction transaction);
        Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId);
        Task<IEnumerable<Transaction>> GetMonthlyStatementAsync(Guid accountId, int year, int month);
    }
}

using BankingSystem.Models.Entity;

namespace BankingSystem.Services.Interface
{
    public interface ITransactionService
    {
        Task<Transaction> DepositAsync(Guid accountId, decimal amount, string reference);
        Task<Transaction> WithdrawAsync(Guid accountId, decimal amount, string reference);
        Task<Transaction> TransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount, string reference);
        Task<IEnumerable<Transaction>> GetTransactionsByAccountAsync(Guid accountId);
        Task<IEnumerable<Transaction>> GetMonthlyStatementAsync(Guid accountId, int year, int month);
    }
}

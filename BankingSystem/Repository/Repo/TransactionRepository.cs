using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using Dapper;
using System.Data;

namespace BankingSystem.Repository.Repo
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IDbConnection _db;

        public TransactionRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            transaction.Id = Guid.NewGuid();
            var sql = @"INSERT INTO Transactions (Id, AccountId, TransactionType, Amount, Reference, CreatedAt)
                        VALUES (@Id, @AccountId, @TransactionType, @Amount, @Reference, @CreatedAt)";
            await _db.ExecuteAsync(sql, transaction);
            return transaction;
        }

        public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId)
        {
            var sql = "SELECT * FROM Transactions WHERE AccountId = @AccountId ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<Transaction>(sql, new { AccountId = accountId });
        }

        public async Task<IEnumerable<Transaction>> GetMonthlyStatementAsync(Guid accountId, int year, int month)
        {
            var sql = @"SELECT * FROM Transactions
                        WHERE AccountId = @AccountId 
                        AND YEAR(CreatedAt) = @Year 
                        AND MONTH(CreatedAt) = @Month
                        ORDER BY CreatedAt DESC";
            return await _db.QueryAsync<Transaction>(sql, new { AccountId = accountId, Year = year, Month = month });
        }
    }
}

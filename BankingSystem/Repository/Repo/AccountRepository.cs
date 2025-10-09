using BankingSystem.Models.Entity;
using BankingSystem.Repository.Interface;
using Dapper;
using System.Data;

namespace BankingSystem.Repository.Repo
{
    public class AccountRepository : IAccountRepository
    {
        private readonly IDbConnection _db;
        public AccountRepository(IDbConnection db)
        {
            _db = db;
        }

        public async Task<Account> CreateAccountAsync(Account account)
        {
            account.Id = Guid.NewGuid();
            var sql = @"INSERT INTO Accounts (Id, UserId, AccountNumber, Currency, Balance, CreatedAt)
                        VALUES (@Id, @UserId, @AccountNumber, @Currency, @Balance, @CreatedAt)";
            await _db.ExecuteAsync(sql, account);
            return account;
        }

        public async Task<Account?> GetByIdAsync(Guid id)
        {
            var sql = "SELECT * FROM Accounts WHERE Id = @Id";
            return await _db.QuerySingleOrDefaultAsync<Account>(sql, new {Id = id});
        }
        
        public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
        {
            var sql = "SELECT * FROM Accounts WHERE AccountNumber = @AccountNumber";
            return await _db.QuerySingleOrDefaultAsync<Account>(sql, new {AccountNumber = accountNumber});
        }
        
        public async Task<IEnumerable<Account>> GetByUserIdAsync(Guid userId)
        {
            var sql = "SELECT * FROM Accounts WHERE UserId = @UserId";
            return await _db.QueryAsync<Account>(sql, new {UserId = userId});
        }

        public async Task UpdateBalanceAsync(Guid accountId, decimal newBalance)
        {
            var sql = "UPDATE Accounts SET Balance = @Balance  WHERE Id = @Id";
            await _db.ExecuteAsync(sql, new { Balance = newBalance, Id = accountId});
        }
    }
}
